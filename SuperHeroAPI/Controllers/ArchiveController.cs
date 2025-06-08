using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgreSQL.Data;
using System.Data;
using System.Text.Json;

namespace SuperHeroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ArchiveController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ArchiveController> _logger;

        public ArchiveController(DataContext context, ILogger<ArchiveController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetArchiveStatus()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();

                // Check if archive schema exists
                using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 
                        FROM information_schema.schemata 
                        WHERE schema_name = 'archive'
                    );";
                
                var schemaExists = (bool)await schemaCommand.ExecuteScalarAsync();
                
                if (!schemaExists)
                {
                    return Ok(new
                    {
                        Status = "Not Initialized",
                        Message = "Archive schema does not exist yet. It will be created when the first archive operation runs."
                    });
                }
                
                // Get archive statistics
                using var statsCommand = connection.CreateCommand();
                statsCommand.CommandText = @"
                    SELECT
                        COUNT(*) AS total_records,
                        MIN(archived_at) AS oldest_record,
                        MAX(archived_at) AS newest_record,
                        COUNT(DISTINCT original_table) AS unique_tables,
                        COUNT(DISTINCT original_schema) AS unique_schemas
                    FROM
                        archive.archived_records;";
                
                using var reader = await statsCommand.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        Status = "Active",
                        TotalRecords = reader.IsDBNull(0) ? 0 : reader.GetInt64(0),
                        OldestRecord = reader.IsDBNull(1) ? null : reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss"),
                        NewestRecord = reader.IsDBNull(2) ? null : reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss"),
                        UniqueTables = reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                        UniqueSchemas = reader.IsDBNull(4) ? 0 : reader.GetInt64(4)
                    });
                }
                
                return Ok(new
                {
                    Status = "Active",
                    TotalRecords = 0,
                    Message = "Archive is initialized but contains no records"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archive status");
                return StatusCode(500, new { Error = "Error getting archive status", Message = ex.Message });
            }
        }

        [HttpGet("records")]
        public async Task<IActionResult> GetArchivedRecords(
            [FromQuery] string? schema = null,
            [FromQuery] string? table = null,
            [FromQuery] string? id = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();
                
                // Check if archive schema exists
                using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 
                        FROM information_schema.schemata 
                        WHERE schema_name = 'archive'
                    );";
                
                var schemaExists = (bool)await checkCommand.ExecuteScalarAsync();
                
                if (!schemaExists)
                {
                    return NotFound("Archive schema has not been created yet");
                }

                // Build query with optional filters
                var queryBuilder = new System.Text.StringBuilder(@"
                    SELECT
                        archive_id,
                        original_schema,
                        original_table,
                        original_id,
                        record_data,
                        archive_reason,
                        archived_at,
                        related_to_archive_id
                    FROM
                        archive.archived_records
                    WHERE 1=1");
                
                var parameters = new List<NpgsqlParameter>();
                
                if (!string.IsNullOrEmpty(schema))
                {
                    queryBuilder.Append(" AND original_schema = @schema");
                    
                    var schemaParam = new NpgsqlParameter("@schema", NpgsqlTypes.NpgsqlDbType.Varchar);
                    schemaParam.Value = schema;
                    parameters.Add(schemaParam);
                }
                
                if (!string.IsNullOrEmpty(table))
                {
                    queryBuilder.Append(" AND original_table = @table");
                    
                    var tableParam = new NpgsqlParameter("@table", NpgsqlTypes.NpgsqlDbType.Varchar);
                    tableParam.Value = table;
                    parameters.Add(tableParam);
                }
                
                if (!string.IsNullOrEmpty(id))
                {
                    queryBuilder.Append(" AND original_id = @id");
                    
                    var idParam = new NpgsqlParameter("@id", NpgsqlTypes.NpgsqlDbType.Varchar);
                    idParam.Value = id;
                    parameters.Add(idParam);
                }
                
                // Add pagination
                queryBuilder.Append(@" 
                    ORDER BY archived_at DESC
                    LIMIT @pageSize 
                    OFFSET @offset");
                
                var pageSizeParam = new NpgsqlParameter("@pageSize", NpgsqlTypes.NpgsqlDbType.Integer);
                pageSizeParam.Value = pageSize;
                parameters.Add(pageSizeParam);
                
                var offsetParam = new NpgsqlParameter("@offset", NpgsqlTypes.NpgsqlDbType.Integer);
                offsetParam.Value = (page - 1) * pageSize;
                parameters.Add(offsetParam);
                
                // Get total count for pagination
                var countBuilder = new System.Text.StringBuilder(@"
                    SELECT COUNT(*)
                    FROM archive.archived_records
                    WHERE 1=1");
                
                if (!string.IsNullOrEmpty(schema))
                {
                    countBuilder.Append(" AND original_schema = @schema");
                }
                
                if (!string.IsNullOrEmpty(table))
                {
                    countBuilder.Append(" AND original_table = @table");
                }
                
                if (!string.IsNullOrEmpty(id))
                {
                    countBuilder.Append(" AND original_id = @id");
                }
                
                using var countCommand = connection.CreateCommand();
                countCommand.CommandText = countBuilder.ToString();
                
                foreach (var param in parameters.Where(p => p.ParameterName != "@pageSize" && p.ParameterName != "@offset"))
                {
                    countCommand.Parameters.Add(param);
                }
                
                var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                // Execute the main query
                using var command = connection.CreateCommand();
                command.CommandText = queryBuilder.ToString();
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }
                
                var records = new List<object>();
                
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var recordJson = reader.GetString(reader.GetOrdinal("record_data"));
                    var recordData = JsonDocument.Parse(recordJson).RootElement;
                    
                    records.Add(new
                    {
                        ArchiveId = reader.GetInt32(reader.GetOrdinal("archive_id")),
                        OriginalSchema = reader.GetString(reader.GetOrdinal("original_schema")),
                        OriginalTable = reader.GetString(reader.GetOrdinal("original_table")),
                        OriginalId = reader.GetString(reader.GetOrdinal("original_id")),
                        ArchiveReason = reader.GetString(reader.GetOrdinal("archive_reason")),
                        ArchivedAt = reader.GetDateTime(reader.GetOrdinal("archived_at")),
                        RelatedToArchiveId = reader.IsDBNull(reader.GetOrdinal("related_to_archive_id")) 
                            ? null 
                            : (int?)reader.GetInt32(reader.GetOrdinal("related_to_archive_id")),
                        Data = recordData
                    });
                }
                
                return Ok(new
                {
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize,
                    Records = records
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving archived records");
                return StatusCode(500, new { Error = "Error retrieving archived records", Message = ex.Message });
            }
        }

        [HttpGet("restore/{id}")]
        public async Task<IActionResult> RestoreArchivedRecord(int id)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();
                
                // Begin transaction
                using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    // Get the archived record
                    using var getCommand = connection.CreateCommand();
                    getCommand.CommandText = @"
                        SELECT 
                            original_schema, 
                            original_table, 
                            original_id, 
                            record_data 
                        FROM 
                            archive.archived_records 
                        WHERE 
                            archive_id = @id;";
                    
                    getCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var idParam = getCommand.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = id;
                    getCommand.Parameters.Add(idParam);
                    
                    using var reader = await getCommand.ExecuteReaderAsync();
                    
                    if (!await reader.ReadAsync())
                    {
                        return NotFound($"Archived record with ID {id} not found");
                    }
                    
                    var originalSchema = reader.GetString(reader.GetOrdinal("original_schema"));
                    var originalTable = reader.GetString(reader.GetOrdinal("original_table"));
                    var originalId = reader.GetString(reader.GetOrdinal("original_id"));
                    var recordData = reader.GetString(reader.GetOrdinal("record_data"));
                    
                    reader.Close();
                    
                    // Check if original table exists
                    using var tableCheckCommand = connection.CreateCommand();
                    tableCheckCommand.CommandText = @"
                        SELECT EXISTS (
                            SELECT 1 
                            FROM information_schema.tables 
                            WHERE table_schema = @schema
                            AND table_name = @table
                        );";
                    
                    tableCheckCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var schemaCheckParam = tableCheckCommand.CreateParameter();
                    schemaCheckParam.ParameterName = "@schema";
                    schemaCheckParam.Value = originalSchema;
                    tableCheckCommand.Parameters.Add(schemaCheckParam);
                    
                    var tableCheckParam = tableCheckCommand.CreateParameter();
                    tableCheckParam.ParameterName = "@table";
                    tableCheckParam.Value = originalTable;
                    tableCheckCommand.Parameters.Add(tableCheckParam);
                    
                    var tableExists = (bool)await tableCheckCommand.ExecuteScalarAsync();
                    
                    if (!tableExists)
                    {
                        return BadRequest($"Original table {originalSchema}.{originalTable} no longer exists");
                    }
                    
                    // Parse the JSON to extract column names and values
                    var recordJson = JsonDocument.Parse(recordData).RootElement;
                    var columnsBuilder = new System.Text.StringBuilder();
                    var valuesBuilder = new System.Text.StringBuilder();
                    var index = 0;
                    var parameters = new List<NpgsqlParameter>();
                    
                    foreach (var property in recordJson.EnumerateObject())
                    {
                        if (index > 0)
                        {
                            columnsBuilder.Append(", ");
                            valuesBuilder.Append(", ");
                        }
                        
                        columnsBuilder.Append($"\"{property.Name}\"");
                        valuesBuilder.Append($"@p{index}");
                        
                        var param = new NpgsqlParameter($"@p{index}", NpgsqlTypes.NpgsqlDbType.Varchar);
                        
                        if (property.Value.ValueKind == JsonValueKind.Null)
                        {
                            param.Value = DBNull.Value;
                        }
                        else
                        {
                            switch (property.Value.ValueKind)
                            {
                                case JsonValueKind.String:
                                    param.Value = property.Value.GetString();
                                    break;
                                case JsonValueKind.Number:
                                    param.Value = property.Value.ToString();
                                    break;
                                case JsonValueKind.True:
                                    param.Value = true;
                                    break;
                                case JsonValueKind.False:
                                    param.Value = false;
                                    break;
                                default:
                                    param.Value = property.Value.ToString();
                                    break;
                            }
                        }
                        
                        parameters.Add(param);
                        index++;
                    }
                    
                    // Insert record back into original table
                    using var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = $@"
                        INSERT INTO {originalSchema}.{originalTable} (
                            {columnsBuilder}
                        ) VALUES (
                            {valuesBuilder}
                        );";
                    
                    insertCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    foreach (var param in parameters)
                    {
                        insertCommand.Parameters.Add(param);
                    }
                    
                    await insertCommand.ExecuteNonQueryAsync();
                    
                    // Mark archive record as restored
                    using var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = @"
                        UPDATE archive.archived_records
                        SET archive_reason = 'RESTORED'
                        WHERE archive_id = @id;";
                    
                    updateCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var updateIdParam = updateCommand.CreateParameter();
                    updateIdParam.ParameterName = "@id";
                    updateIdParam.Value = id;
                    updateCommand.Parameters.Add(updateIdParam);
                    
                    await updateCommand.ExecuteNonQueryAsync();
                    
                    // Also restore any related records that were archived with this one
                    using var relatedCommand = connection.CreateCommand();
                    relatedCommand.CommandText = @"
                        SELECT archive_id
                        FROM archive.archived_records
                        WHERE related_to_archive_id = @id;";
                    
                    relatedCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var relatedIdParam = relatedCommand.CreateParameter();
                    relatedIdParam.ParameterName = "@id";
                    relatedIdParam.Value = id;
                    relatedCommand.Parameters.Add(relatedIdParam);
                    
                    var relatedIds = new List<int>();
                    
                    using var relatedReader = await relatedCommand.ExecuteReaderAsync();
                    while (await relatedReader.ReadAsync())
                    {
                        relatedIds.Add(relatedReader.GetInt32(0));
                    }
                    
                    relatedReader.Close();
                    
                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    // Recursively restore related records (if any)
                    foreach (var relatedId in relatedIds)
                    {
                        await RestoreArchivedRecord(relatedId);
                    }
                    
                    return Ok(new
                    {
                        Message = $"Successfully restored record {id} to {originalSchema}.{originalTable}",
                        RestoredId = originalId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error restoring archived record {id}");
                    return StatusCode(500, new { Error = $"Error restoring archived record {id}", Message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening connection to restore archived record {id}");
                return StatusCode(500, new { Error = $"Error opening connection to restore archived record {id}", Message = ex.Message });
            }
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerArchiving([FromQuery] string? specificTable = null)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                    await connection.OpenAsync();
                
                if (string.IsNullOrEmpty(specificTable))
                {
                    // Process all tables in TableUser
                    var tableUsers = await _context.TableUsers.ToListAsync();
                    
                    foreach (var tableUser in tableUsers)
                    {
                        if (string.IsNullOrEmpty(tableUser.Tablename))
                            continue;
                            
                        // Parse schema and table name
                        var parts = tableUser.Tablename.Split('.');
                        if (parts.Length != 2)
                            continue;
                            
                        var schemaName = parts[0];
                        var tableName = parts[1];
                        
                        // Check if table has deleted_at column
                        using var columnCheckCommand = connection.CreateCommand();
                        columnCheckCommand.CommandText = @"
                            SELECT EXISTS (
                                SELECT 1
                                FROM information_schema.columns
                                WHERE table_schema = @schema
                                AND table_name = @table
                                AND column_name = 'deleted_at'
                            );";
                        
                        var schemaParam = columnCheckCommand.CreateParameter();
                        schemaParam.ParameterName = "@schema";
                        schemaParam.Value = schemaName;
                        columnCheckCommand.Parameters.Add(schemaParam);
                        
                        var tableParam = columnCheckCommand.CreateParameter();
                        tableParam.ParameterName = "@table";
                        tableParam.Value = tableName;
                        columnCheckCommand.Parameters.Add(tableParam);
                        
                        var hasDeletedAt = (bool)await columnCheckCommand.ExecuteScalarAsync();
                        
                        if (!hasDeletedAt)
                            continue;
                        
                        // Archive old deleted records
                        await TriggerArchivingForTable(connection, schemaName, tableName);
                    }
                    
                    return Ok(new { Message = "Archive process triggered for all eligible tables" });
                }
                else
                {
                    // Process specific table
                    var parts = specificTable.Split('.');
                    if (parts.Length != 2)
                    {
                        return BadRequest("Table name must be in format 'schema.table'");
                    }
                    
                    var schemaName = parts[0];
                    var tableName = parts[1];
                    
                    // Check if table exists
                    using var tableCheckCommand = connection.CreateCommand();
                    tableCheckCommand.CommandText = @"
                        SELECT EXISTS (
                            SELECT 1
                            FROM information_schema.tables
                            WHERE table_schema = @schema
                            AND table_name = @table
                        );";
                    
                    var schemaParam = tableCheckCommand.CreateParameter();
                    schemaParam.ParameterName = "@schema";
                    schemaParam.Value = schemaName;
                    tableCheckCommand.Parameters.Add(schemaParam);
                    
                    var tableParam = tableCheckCommand.CreateParameter();
                    tableParam.ParameterName = "@table";
                    tableParam.Value = tableName;
                    tableCheckCommand.Parameters.Add(tableParam);
                    
                    var tableExists = (bool)await tableCheckCommand.ExecuteScalarAsync();
                    
                    if (!tableExists)
                    {
                        return NotFound($"Table {specificTable} does not exist");
                    }
                    
                    // Check if table has deleted_at column
                    using var columnCheckCommand = connection.CreateCommand();
                    columnCheckCommand.CommandText = @"
                        SELECT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = @schema
                            AND table_name = @table
                            AND column_name = 'deleted_at'
                        );";
                    
                    var columnSchemaParam = columnCheckCommand.CreateParameter();
                    columnSchemaParam.ParameterName = "@schema";
                    columnSchemaParam.Value = schemaName;
                    columnCheckCommand.Parameters.Add(columnSchemaParam);
                    
                    var columnTableParam = columnCheckCommand.CreateParameter();
                    columnTableParam.ParameterName = "@table";
                    columnTableParam.Value = tableName;
                    columnCheckCommand.Parameters.Add(columnTableParam);
                    
                    var hasDeletedAt = (bool)await columnCheckCommand.ExecuteScalarAsync();
                    
                    if (!hasDeletedAt)
                    {
                        return BadRequest($"Table {specificTable} does not have a deleted_at column for soft deletes");
                    }
                    
                    // Archive old deleted records
                    await TriggerArchivingForTable(connection, schemaName, tableName);
                    
                    return Ok(new { Message = $"Archive process triggered for table {specificTable}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering archive process");
                return StatusCode(500, new { Error = "Error triggering archive process", Message = ex.Message });
            }
        }

        private async Task TriggerArchivingForTable(DbConnection connection, string schemaName, string tableName)
        {
            // Ensure archive schema exists
            using var createSchemaCommand = connection.CreateCommand();
            createSchemaCommand.CommandText = @"
                CREATE SCHEMA IF NOT EXISTS archive;
                
                CREATE TABLE IF NOT EXISTS archive.archived_records (
                    archive_id SERIAL PRIMARY KEY,
                    original_schema VARCHAR(255) NOT NULL,
                    original_table VARCHAR(255) NOT NULL,
                    original_id VARCHAR(255) NOT NULL,
                    record_data JSONB NOT NULL,
                    archive_reason VARCHAR(50) NOT NULL,
                    archived_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    related_to_archive_id INTEGER,
                    CONSTRAINT fk_related_archived_record
                        FOREIGN KEY (related_to_archive_id)
                        REFERENCES archive.archived_records(archive_id)
                        ON DELETE SET NULL
                );
                
                CREATE INDEX IF NOT EXISTS idx_archived_records_original 
                ON archive.archived_records(original_schema, original_table, original_id);
                
                CREATE INDEX IF NOT EXISTS idx_archived_records_archived_at
                ON archive.archived_records(archived_at);";
            
            await createSchemaCommand.ExecuteNonQueryAsync();
            
            // Get primary key column
            using var pkCommand = connection.CreateCommand();
            pkCommand.CommandText = @"
                SELECT a.attname
                FROM pg_index i
                JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
                JOIN pg_namespace n ON n.oid = i.indnamespace
                JOIN pg_class c ON i.indrelid = c.oid
                JOIN pg_namespace nc ON nc.oid = c.relnamespace
                WHERE i.indisprimary
                AND nc.nspname = @schema
                AND c.relname = @table
                LIMIT 1;";
            
            var pkSchemaParam = pkCommand.CreateParameter();
            pkSchemaParam.ParameterName = "@schema";
            pkSchemaParam.Value = schemaName;
            pkCommand.Parameters.Add(pkSchemaParam);
            
            var pkTableParam = pkCommand.CreateParameter();
            pkTableParam.ParameterName = "@table";
            pkTableParam.Value = tableName;
            pkCommand.Parameters.Add(pkTableParam);
            
            var pkColumn = await pkCommand.ExecuteScalarAsync() as string;
            
            if (string.IsNullOrEmpty(pkColumn))
            {
                _logger.LogWarning($"Could not determine primary key for {schemaName}.{tableName}");
                return;
            }
            
            // Find records to archive (deleted more than 1 month ago)
            using var recordsCommand = connection.CreateCommand();
            recordsCommand.CommandText = $@"
                SELECT {pkColumn}::text
                FROM {schemaName}.{tableName}
                WHERE deleted_at IS NOT NULL
                AND deleted_at < (CURRENT_DATE - INTERVAL '1 month');";
            
            var recordIds = new List<string>();
            
            using var reader = await recordsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recordIds.Add(reader.GetString(0));
            }
            
            // Archive each record
            foreach (var recordId in recordIds)
            {
                using var transaction = await connection.BeginTransactionAsync();
                
                try
                {
                    // Get record as JSON
                    using var jsonCommand = connection.CreateCommand();
                    jsonCommand.CommandText = $@"
                        SELECT row_to_json({tableName})
                        FROM {schemaName}.{tableName}
                        WHERE {pkColumn} = @id;";
                    
                    jsonCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var jsonIdParam = jsonCommand.CreateParameter();
                    jsonIdParam.ParameterName = "@id";
                    jsonIdParam.Value = recordId;
                    jsonCommand.Parameters.Add(jsonIdParam);
                    
                    var jsonData = await jsonCommand.ExecuteScalarAsync() as string;
                    
                    if (string.IsNullOrEmpty(jsonData))
                    {
                        _logger.LogWarning($"Record {recordId} not found in {schemaName}.{tableName}");
                        await transaction.RollbackAsync();
                        continue;
                    }
                    
                    // Archive record
                    using var archiveCommand = connection.CreateCommand();
                    archiveCommand.CommandText = @"
                        INSERT INTO archive.archived_records
                            (original_schema, original_table, original_id, record_data, archive_reason)
                        VALUES
                            (@schema, @table, @id, @data::jsonb, 'SOFT_DELETE_EXPIRED')
                        RETURNING archive_id;";
                    
                    archiveCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var archiveSchemaParam = archiveCommand.CreateParameter();
                    archiveSchemaParam.ParameterName = "@schema";
                    archiveSchemaParam.Value = schemaName;
                    archiveCommand.Parameters.Add(archiveSchemaParam);
                    
                    var archiveTableParam = archiveCommand.CreateParameter();
                    archiveTableParam.ParameterName = "@table";
                    archiveTableParam.Value = tableName;
                    archiveCommand.Parameters.Add(archiveTableParam);
                    
                    var archiveIdParam = archiveCommand.CreateParameter();
                    archiveIdParam.ParameterName = "@id";
                    archiveIdParam.Value = recordId;
                    archiveCommand.Parameters.Add(archiveIdParam);
                    
                    var archiveDataParam = archiveCommand.CreateParameter();
                    archiveDataParam.ParameterName = "@data";
                    archiveDataParam.Value = jsonData;
                    archiveCommand.Parameters.Add(archiveDataParam);
                    
                    var archiveId = await archiveCommand.ExecuteScalarAsync();
                    
                    // Delete original record
                    using var deleteCommand = connection.CreateCommand();
                    deleteCommand.CommandText = $@"
                        DELETE FROM {schemaName}.{tableName}
                        WHERE {pkColumn} = @id;";
                    
                    deleteCommand.Transaction = transaction as NpgsqlTransaction;
                    
                    var deleteIdParam = deleteCommand.CreateParameter();
                    deleteIdParam.ParameterName = "@id";
                    deleteIdParam.Value = recordId;
                    deleteCommand.Parameters.Add(deleteIdParam);
                    
                    await deleteCommand.ExecuteNonQueryAsync();
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"Archived record {recordId} from {schemaName}.{tableName}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error archiving record {recordId} from {schemaName}.{tableName}");
                }
            }
            
            // Clean up old archived records
            using var cleanupCommand = connection.CreateCommand();
            cleanupCommand.CommandText = @"
                DELETE FROM archive.archived_records
                WHERE archived_at < (CURRENT_DATE - INTERVAL '1 year');";
            
            var deletedCount = await cleanupCommand.ExecuteNonQueryAsync();
            _logger.LogInformation($"Cleaned up {deletedCount} archived records older than 1 year");
        }
    }
} 