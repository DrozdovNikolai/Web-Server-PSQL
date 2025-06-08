using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PostgreSQL.Data;
using SuperHeroAPI.md2;

namespace SuperHeroAPI.Services
{
    public class DataArchivingService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;
        private readonly ILogger<DataArchivingService> _logger;

        public DataArchivingService(
            IServiceProvider serviceProvider,
            ILogger<DataArchivingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Data Archiving Service is starting.");

            // Run once at startup, then on a schedule
            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromHours(24)); // Run daily

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            _logger.LogInformation("Data Archiving Service is working.");
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                
                // Get all tables from TableUser
                var tableUsers = await dbContext.TableUsers
                    .Include(tu => tu.User)
                    .ToListAsync();

                // Process each table to check for soft deleted records
                foreach (var tableUser in tableUsers)
                {
                    if (string.IsNullOrEmpty(tableUser.Tablename))
                        continue;

                    await ProcessTableForArchiving(dbContext, tableUser.Tablename);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing data archiving.");
            }
        }

        private async Task ProcessTableForArchiving(DataContext dbContext, string fullTableName)
        {
            _logger.LogInformation($"Processing table {fullTableName} for archiving");
            
            try
            {
                // Parse schema and table name
                var tableComponents = ParseTableName(fullTableName);
                var schemaName = tableComponents.Item1;
                var tableName = tableComponents.Item2;
                
                if (string.IsNullOrEmpty(schemaName) || string.IsNullOrEmpty(tableName))
                {
                    _logger.LogWarning($"Invalid table name format: {fullTableName}");
                    return;
                }
                
                // Check if table has deleted_at column
                if (!await HasDeletedAtColumn(dbContext, schemaName, tableName))
                {
                    _logger.LogInformation($"Table {fullTableName} does not have deleted_at column, skipping");
                    return;
                }
                
                // Archive records older than one month
                await ArchiveOldRecords(dbContext, schemaName, tableName);
                
                // Permanently delete archived records older than one year
                await DeleteOldArchivedRecords(dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing table {fullTableName}");
            }
        }

        private (string, string) ParseTableName(string fullTableName)
        {
            var parts = fullTableName.Split('.');
            if (parts.Length == 2)
            {
                return (parts[0], parts[1]);
            }
            return (string.Empty, fullTableName); // Default to public schema if not specified
        }

        private async Task<bool> HasDeletedAtColumn(DataContext dbContext, string schemaName, string tableName)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE table_schema = @schema
                    AND table_name = @table
                    AND column_name = 'deleted_at'
                );";
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@schema";
            schemaParam.Value = schemaName;
            command.Parameters.Add(schemaParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);
            
            return (bool)await command.ExecuteScalarAsync();
        }

        private async Task ArchiveOldRecords(DataContext dbContext, string schemaName, string tableName)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            // Create archive table if it doesn't exist
            await EnsureArchiveTableExists(dbContext);
            
            // Get primary key column of the source table
            var primaryKeyColumn = await GetPrimaryKeyColumn(dbContext, schemaName, tableName);
            if (string.IsNullOrEmpty(primaryKeyColumn))
            {
                _logger.LogWarning($"Could not determine primary key for {schemaName}.{tableName}");
                return;
            }
            
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            
            try
            {
                // Get list of records to archive (deleted more than 1 month ago)
                var recordsToArchive = await GetRecordsToArchive(dbContext, schemaName, tableName);
                
                foreach (var recordId in recordsToArchive)
                {
                    // Archive the main record
                    await ArchiveRecord(dbContext, schemaName, tableName, primaryKeyColumn, recordId);
                    
                    // Archive related records (foreign key relationships)
                    await ArchiveRelatedRecords(dbContext, schemaName, tableName, primaryKeyColumn, recordId);
                }
                
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error archiving records from {schemaName}.{tableName}");
                throw;
            }
        }

        private async Task EnsureArchiveTableExists(DataContext dbContext)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
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
                ON archive.archived_records(archived_at);
            ";
            
            await command.ExecuteNonQueryAsync();
        }

        private async Task<string> GetPrimaryKeyColumn(DataContext dbContext, string schemaName, string tableName)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT a.attname
                FROM pg_index i
                JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
                JOIN pg_namespace n ON n.oid = i.indnamespace
                JOIN pg_class c ON i.indrelid = c.oid
                JOIN pg_namespace nc ON nc.oid = c.relnamespace
                WHERE i.indisprimary
                AND nc.nspname = @schema
                AND c.relname = @table
                LIMIT 1;
            ";
            
            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@schema";
            schemaParam.Value = schemaName;
            command.Parameters.Add(schemaParam);
            
            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);
            
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? string.Empty;
        }

        private async Task<List<string>> GetRecordsToArchive(DataContext dbContext, string schemaName, string tableName)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            var primaryKeyColumn = await GetPrimaryKeyColumn(dbContext, schemaName, tableName);
            
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {primaryKeyColumn}::text
                FROM {schemaName}.{tableName}
                WHERE deleted_at IS NOT NULL
                AND deleted_at < (CURRENT_DATE - INTERVAL '1 month');
            ";
            
            var results = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }
            
            return results;
        }

        private async Task ArchiveRecord(DataContext dbContext, string schemaName, string tableName, string primaryKeyColumn, string recordId)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            // Get full record data as JSON
            using var getRecordCommand = connection.CreateCommand();
            getRecordCommand.CommandText = $@"
                SELECT row_to_json({tableName})
                FROM {schemaName}.{tableName}
                WHERE {primaryKeyColumn} = @recordId;
            ";
            
            var recordIdParam = getRecordCommand.CreateParameter();
            recordIdParam.ParameterName = "@recordId";
            recordIdParam.Value = recordId;
            getRecordCommand.Parameters.Add(recordIdParam);
            
            var recordJson = await getRecordCommand.ExecuteScalarAsync() as string;
            if (string.IsNullOrEmpty(recordJson))
            {
                _logger.LogWarning($"Record {recordId} not found in {schemaName}.{tableName}");
                return;
            }
            
            // Insert into archive
            using var archiveCommand = connection.CreateCommand();
            archiveCommand.CommandText = @"
                INSERT INTO archive.archived_records
                    (original_schema, original_table, original_id, record_data, archive_reason)
                VALUES
                    (@schema, @table, @recordId, @recordData::jsonb, 'SOFT_DELETE_EXPIRED')
                RETURNING archive_id;
            ";
            
            var schemaParam = archiveCommand.CreateParameter();
            schemaParam.ParameterName = "@schema";
            schemaParam.Value = schemaName;
            archiveCommand.Parameters.Add(schemaParam);
            
            var tableParam = archiveCommand.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            archiveCommand.Parameters.Add(tableParam);
            
            var recordIdArchiveParam = archiveCommand.CreateParameter();
            recordIdArchiveParam.ParameterName = "@recordId";
            recordIdArchiveParam.Value = recordId;
            archiveCommand.Parameters.Add(recordIdArchiveParam);
            
            var recordDataParam = archiveCommand.CreateParameter();
            recordDataParam.ParameterName = "@recordData";
            recordDataParam.Value = recordJson;
            archiveCommand.Parameters.Add(recordDataParam);
            
            var archiveId = await archiveCommand.ExecuteScalarAsync();
            _logger.LogInformation($"Archived record {recordId} from {schemaName}.{tableName} to archive ID {archiveId}");
            
            // Delete from original table
            using var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = $@"
                DELETE FROM {schemaName}.{tableName}
                WHERE {primaryKeyColumn} = @recordId;
            ";
            
            var deleteRecordIdParam = deleteCommand.CreateParameter();
            deleteRecordIdParam.ParameterName = "@recordId";
            deleteRecordIdParam.Value = recordId;
            deleteCommand.Parameters.Add(deleteRecordIdParam);
            
            await deleteCommand.ExecuteNonQueryAsync();
        }

        private async Task ArchiveRelatedRecords(DataContext dbContext, string schemaName, string tableName, 
            string primaryKeyColumn, string recordId)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            // Find all foreign key relationships pointing to this table
            using var findRelationsCommand = connection.CreateCommand();
            findRelationsCommand.CommandText = @"
                SELECT
                    ns.nspname AS source_schema,
                    src.relname AS source_table,
                    attr.attname AS source_column,
                    conrelid::regclass AS source_table_oid,
                    confrelid::regclass AS target_table_oid,
                    (SELECT attname FROM pg_attribute WHERE attrelid = conrelid AND attnum = conkey[1]) AS source_column_name,
                    (SELECT attname FROM pg_attribute WHERE attrelid = confrelid AND attnum = confkey[1]) AS target_column_name
                FROM pg_constraint
                JOIN pg_namespace ns ON ns.oid = connamespace
                JOIN pg_class src ON src.oid = conrelid
                JOIN pg_attribute attr ON attr.attrelid = conrelid AND attr.attnum = conkey[1]
                WHERE confrelid = (SELECT c.oid FROM pg_class c 
                                  JOIN pg_namespace n ON n.oid = c.relnamespace 
                                  WHERE c.relname = @table 
                                  AND n.nspname = @schema)
                AND contype = 'f';
            ";
            
            var schemaParam = findRelationsCommand.CreateParameter();
            schemaParam.ParameterName = "@schema";
            schemaParam.Value = schemaName;
            findRelationsCommand.Parameters.Add(schemaParam);
            
            var tableParam = findRelationsCommand.CreateParameter();
            tableParam.ParameterName = "@table";
            tableParam.Value = tableName;
            findRelationsCommand.Parameters.Add(tableParam);
            
            using var reader = await findRelationsCommand.ExecuteReaderAsync();
            var relatedTables = new List<(string schema, string table, string column)>();
            
            while (await reader.ReadAsync())
            {
                relatedTables.Add((
                    reader.GetString(reader.GetOrdinal("source_schema")),
                    reader.GetString(reader.GetOrdinal("source_table")),
                    reader.GetString(reader.GetOrdinal("source_column_name"))
                ));
            }
            
            // Process each related table
            foreach (var (relatedSchema, relatedTable, relatedColumn) in relatedTables)
            {
                // Get related records
                using var getRelatedCommand = connection.CreateCommand();
                getRelatedCommand.CommandText = $@"
                    SELECT {await GetPrimaryKeyColumn(dbContext, relatedSchema, relatedTable)}::text
                    FROM {relatedSchema}.{relatedTable}
                    WHERE {relatedColumn} = @recordId;
                ";
                
                var recordIdParam = getRelatedCommand.CreateParameter();
                recordIdParam.ParameterName = "@recordId";
                recordIdParam.Value = recordId;
                getRelatedCommand.Parameters.Add(recordIdParam);
                
                var relatedRecordIds = new List<string>();
                using var relatedReader = await getRelatedCommand.ExecuteReaderAsync();
                while (await relatedReader.ReadAsync())
                {
                    relatedRecordIds.Add(relatedReader.GetString(0));
                }
                
                // Archive each related record
                var relatedPkColumn = await GetPrimaryKeyColumn(dbContext, relatedSchema, relatedTable);
                foreach (var relatedId in relatedRecordIds)
                {
                    await ArchiveRecord(dbContext, relatedSchema, relatedTable, relatedPkColumn, relatedId);
                    
                    // Recursively archive related records (handle deep relationships)
                    await ArchiveRelatedRecords(dbContext, relatedSchema, relatedTable, relatedPkColumn, relatedId);
                }
            }
        }

        private async Task DeleteOldArchivedRecords(DataContext dbContext)
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();
            
            // Delete records older than 1 year
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM archive.archived_records
                WHERE archived_at < (CURRENT_DATE - INTERVAL '1 year');
            ";
            
            var deletedCount = await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"Permanently deleted {deletedCount} archived records older than 1 year");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Data Archiving Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
} 