using Azure.Core;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Npgsql;
using SuperHeroAPI.md2;

using SuperHeroAPI.Services.SuperHeroService;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using OfficeOpenXml.Style; 


[Route("api/[controller]")]
[ApiController, Authorize]
public class QueryController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IRoleService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryController(DataContext context, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _roleService = roleService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("{tableName}")]

    public async Task<IActionResult> Get(string tableName)
    {
        return await ExecuteQuery(tableName, "SELECT * FROM " + tableName + ";");
    }

    [HttpPost("{tableName}")]

    public async Task<IActionResult> Post(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"INSERT INTO {tableName} {queryRequest.Query}");
    }

    [HttpPut("{tableName}")]

    public async Task<IActionResult> Put(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"UPDATE {tableName} SET {queryRequest.Query}");
    }

    [HttpDelete("{tableName}")]

    public async Task<IActionResult> Delete(string tableName, [FromBody] QueryRequest queryRequest)
    {
        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        return await ExecuteQuery(tableName, $"DELETE FROM {tableName} WHERE {queryRequest.Query}");
    }
    private async Task<IActionResult> ExecuteQuery(string tableName, string query)
    {
        try
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                // Set the role based on the user's role received from the claim
                command.CommandText = $"SET ROLE {userRole}; {query}";
                command.CommandType = CommandType.Text;
                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }



                    return Ok(result);
                }
            }
        }
        catch (DbException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    // Method to create a table in PostgreSQL
    [HttpPost("CreatePsqlTable")]
    public async Task<IActionResult> CreatePsqlTable([FromBody] TableCreationRequest request)
    {
        if (string.IsNullOrEmpty(request.TableName))
        {
            return BadRequest("Table name is required.");
        }

        // Build the CREATE TABLE statement
        string createTableSql = $"CREATE TABLE {request.TableName} (";
        List<string> columnDefinitions = new List<string>();
        foreach (var column in request.Columns)
        {
            // Validate column name and data type
            if (string.IsNullOrEmpty(column.ColumnName))
            {
                return BadRequest("Column name is required.");
            }
            if (string.IsNullOrEmpty(column.DataType))
            {
                return BadRequest("Data type is required for column: " + column.ColumnName);
            }

            // Add column definition to the SQL statement
            if (column.DataType.ToLower().Contains("serial"))
            {
                columnDefinitions.Add($"{column.ColumnName} SERIAL");
                columnDefinitions.Add($"PRIMARY KEY ({column.ColumnName})"); // Add PRIMARY KEY constraint
            }
            else
            {
                columnDefinitions.Add($"{column.ColumnName} {column.DataType}");
            }
        }

        createTableSql += string.Join(", ", columnDefinitions);
        createTableSql += ")";

        // Execute the SQL statement
        try
        {
            await _context.Database.ExecuteSqlRawAsync(createTableSql);
            return Ok($"Table '{request.TableName}' created successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating table: {ex.Message}");
        }
    }
    [HttpPost("CreatePsqlTable2")]
    public async Task<IActionResult> CreatePsqlTable2([FromBody] TableCreationRequest2 request)
    {
        if (string.IsNullOrEmpty(request.TableName))
        {
            return BadRequest("Table name is required.");
        }

        // Build the CREATE TABLE statement
        string createTableSql = $"CREATE TABLE {request.TableName} (";
        List<string> columnDefinitions = new List<string>();

        foreach (var column in request.Columns)
        {
            // Validate column name and data type
            if (string.IsNullOrEmpty(column.ColumnName))
            {
                return BadRequest("Column name is required.");
            }
            if (string.IsNullOrEmpty(column.DataType))
            {
                return BadRequest("Data type is required for column: " + column.ColumnName);
            }

            // Add column definition to the SQL statement
            columnDefinitions.Add($"{column.ColumnName} {column.DataType}");
        }

        createTableSql += string.Join(", ", columnDefinitions);
        createTableSql += ")";

        // Execute the CREATE TABLE statement
        try
        {
            await _context.Database.ExecuteSqlRawAsync(createTableSql);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating table: {ex.Message}");
        }

        // Extract username from JWT token
        string username = User.Identity.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized("User is not authorized.");
        }

        // Find the user ID based on the username
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Add a row to the table_user table
        var tableUser = new TableUser
        {
            Tablename = request.TableName,
            UserId = user.Id
        };

        _context.TableUsers.Add(tableUser);

        try
        {
            await _context.SaveChangesAsync();
            return Ok($"Table '{request.TableName}' created successfully and entry added to 'table_user'.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding entry to table_user: {ex.Message}");
        }
    }

    [HttpPost("CreateDynamicProcedures")]
    public async Task<IActionResult> CreateDynamicProcedures([FromBody] string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            return BadRequest("Table name is required.");
        }

        // Detect the primary key column
        string primaryKeyColumn = GetPrimaryKeyColumnName(tableName);
        if (string.IsNullOrEmpty(primaryKeyColumn))
        {
            return BadRequest("Primary key column could not be determined.");
        }

        // Create procedure to insert a new record (excluding primary key)
        var columnDefinitions = GetColumnDefinitionsForTable2(tableName)
            .Where(col => !col.StartsWith(primaryKeyColumn + " ")).ToList();
        var columnNames = GetColumnNamesForTable2(tableName)
            .Where(col => col != primaryKeyColumn).ToList();

        string createInsertProcedureSql = $@"
    CREATE OR REPLACE PROCEDURE insert_{tableName}(
        {string.Join(", ", columnDefinitions)}
    )
    LANGUAGE plpgsql AS $$
    BEGIN
        INSERT INTO {tableName} ({string.Join(", ", columnNames)})
        VALUES ({string.Join(", ", columnNames)});
    END;
    $$;";

        // Create procedure to update a record by primary key (using p_ prefix for parameters)
        string createUpdateProcedureSql = $@"
    CREATE OR REPLACE PROCEDURE update_{tableName}(
        {string.Join(", ", columnDefinitions.Select(c => "p_" + c))}
    )
    LANGUAGE plpgsql AS $$
    BEGIN
        UPDATE {tableName}
        SET {string.Join(", ", columnNames.Select(c => $"{c} = p_{c}"))}
        WHERE {primaryKeyColumn} = p_{primaryKeyColumn};
    END;
    $$;";

        // Create procedure to "delete" a record by primary key (set deleted_at to current timestamp)
        string createDeleteProcedureSql = $@"
    CREATE OR REPLACE PROCEDURE delete_{tableName}(
        p_{primaryKeyColumn} INTEGER
    )
    LANGUAGE plpgsql AS $$
    BEGIN
        UPDATE {tableName} SET deleted_at = now() WHERE {primaryKeyColumn} = p_{primaryKeyColumn};
    END;
    $$;";

        // Create procedure to select data from the table
        string createSelectProcedureSql = $@"
    CREATE OR REPLACE FUNCTION select_{tableName}()
    RETURNS JSON AS $$
    BEGIN
        RETURN (SELECT json_agg(row_to_json(t)) FROM (SELECT * FROM {tableName}) t);
    END;
    $$ LANGUAGE plpgsql;";

        // Execute SQL statements
        try
        {
            await _context.Database.ExecuteSqlRawAsync(createInsertProcedureSql);
            await _context.Database.ExecuteSqlRawAsync(createUpdateProcedureSql);
            await _context.Database.ExecuteSqlRawAsync(createDeleteProcedureSql);
            await _context.Database.ExecuteSqlRawAsync(createSelectProcedureSql);
        }
        catch (PostgresException pgEx)
        {
            // Return detailed PostgreSQL error message
            return StatusCode(500, new
            {
                success = false,
                message = "Error creating stored procedures.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating stored procedures: {ex.Message}");
        }

        // Extract username from JWT token
        string username = User.Identity.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized("User is not authorized.");
        }

        // Find the user ID based on the username
        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // Add a row to the procedure_user table
        var procedureNames = new[] { "insert", "update", "delete" };
        foreach (var procedureName in procedureNames)
        {
            var procedureUser = new ProcedureUser
            {
                ProcedureName = $"{procedureName}_{tableName}",
                UserId = user.Id
            };

            _context.ProcedureUsers.Add(procedureUser);
        }
        var functionUser = new FunctionUser
        {
            FunctionName = $"select_{tableName}",
            UserId = user.Id
        };

        _context.FunctionUsers.Add(functionUser);
        try
        {
            await _context.SaveChangesAsync();
            return Ok($"Stored procedures for {tableName} table created successfully and entries added to 'procedure_user'.");
        }
        catch (PostgresException pgEx)
        {
            // Return detailed PostgreSQL error message for saving procedure user entries
            return StatusCode(500, new
            {
                success = false,
                message = "Error adding entries to procedure_user.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding entries to procedure_user: {ex.Message}");
        }
    }


    private string GetPrimaryKeyColumnName(string tableName)
    {
        // Execute a SQL query to get the primary key column name
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $@"
        SELECT a.attname
        FROM pg_index i
        JOIN pg_attribute a ON a.attnum = ANY(i.indkey)
        WHERE i.indrelid = '{tableName}'::regclass
        AND i.indisprimary;
    ";
        command.CommandType = CommandType.Text;

        _context.Database.OpenConnection();
        using var reader = command.ExecuteReader();

        string primaryKeyColumn = null;
        if (reader.Read())
        {
            primaryKeyColumn = reader.GetString(0);
        }

        _context.Database.CloseConnection();

        return primaryKeyColumn;
    }

    private List<string> GetColumnDefinitionsForTable2(string tableName)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'";
        command.CommandType = CommandType.Text;

        _context.Database.OpenConnection();
        using var reader = command.ExecuteReader();

        List<string> columnDefinitions = new List<string>();
        while (reader.Read())
        {
            string columnName = reader.GetString(0);
            string dataType = reader.GetString(1);
            columnDefinitions.Add($"{columnName} {dataType}");
        }

        _context.Database.CloseConnection();

        return columnDefinitions;
    }

    private List<string> GetColumnNamesForTable2(string tableName)
    {
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}'";
        command.CommandType = CommandType.Text;

        _context.Database.OpenConnection();
        using var reader = command.ExecuteReader();

        List<string> columnNames = new List<string>();
        while (reader.Read())
        {
            columnNames.Add(reader.GetString(0));
        }

        _context.Database.CloseConnection();

        return columnNames;
    }






    [HttpGet("ListEntities")]
    public IActionResult ListEntities()
    {
        var entityTypes = _context.Model.GetEntityTypes();


        List<string> entityNames = new List<string>();


        foreach (var entityType in entityTypes)
        {
            entityNames.Add(entityType.Name);
        }


        return Ok(entityNames);
    }
   



    [HttpGet("rest/{tableName}")]
    public async Task<IActionResult> RestProcedureGet(string tableName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction($"select_{tableName}", parameters, "GET");
    }

    [HttpPost("rest/{tableName}")]
    public async Task<IActionResult> RestProcedurePost(string tableName, [FromBody] Dictionary<string, object> parameters)
    {
        string procedureName = $"insert_{tableName}";
        return await RestProcedure(procedureName, parameters, "POST");
    }
    [HttpPut("rest/{tableName}")]
    public async Task<IActionResult> RestProcedurePut(string tableName, [FromBody] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"update_{tableName}", parameters, "PUT");
    }

    [HttpDelete("rest/{tableName}")]
    public async Task<IActionResult> RestProcedureDelete(string tableName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"delete_{tableName}", parameters, "DELETE");
    }



    [HttpGet("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNameGet(string procedureName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "GET");
    }

    [HttpPost("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNamePost(string procedureName, [FromBody] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "POST");
    }

    [HttpPut("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNamePut(string procedureName, [FromBody] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "PUT");
    }

    [HttpDelete("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNameDelete(string procedureName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "DELETE");
    }


    [HttpGet("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNameGet(string functionName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction($"{functionName}", parameters, "GET");
    }

    [HttpPost("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePost(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction($"{functionName}", parameters, "POST");
    }

    [HttpPut("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePut(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction($"{functionName}", parameters, "PUT");
    }

    [HttpDelete("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNameDelete(string functionName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction($"{functionName}", parameters, "DELETE");
    }
    [HttpGet("cursor/{functionName}")]
    public async Task<IActionResult> functionNameCursorGet(string functionName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunctionCursor(functionName, parameters, "GET");
    }

    // POST: api/Cursor/cursor/{functionName}
    // Параметры передаются в теле запроса
    [HttpPost("cursor/{functionName}")]
    public async Task<IActionResult> functionNameCursorPost(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunctionCursor(functionName, parameters, "POST");
    }

    // PUT: api/Cursor/cursor/{functionName}
    // Параметры передаются в теле запроса
    [HttpPut("cursor/{functionName}")]
    public async Task<IActionResult> functionNameCursorPut(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunctionCursor(functionName, parameters, "PUT");
    }

    // DELETE: api/Cursor/cursor/{functionName}?param1=value1&...
    [HttpDelete("cursor/{functionName}")]
    public async Task<IActionResult> functionNameCursorDelete(string functionName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunctionCursor(functionName, parameters, "DELETE");
    }

    // Общая логика вызова функции PostgreSQL, возвращающей курсор.
    private async Task<IActionResult> ExecuteFunctionCursor(string functionName, Dictionary<string, object> parameters, string method)
    {
        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            // Получение ролей из JWT токена
            var roles = GetRolesFromJwtToken();
            if (roles == null || roles.Count == 0)
            {
                return StatusCode(403, new { success = false, message = "No roles available to set." });
            }

            // Получение определения параметров функции
            var parameterDefinitions = await GetFunctionParametersAsync(functionName, connection);

            var paramValues = new List<string>();
            foreach (var parameterDefinition in parameterDefinitions)
            {
                //to:do подумать над параметрами
                var paramName = parameterDefinition.ParameterName;
                var paramType = parameterDefinition.DataType;
                var paramValue = parameters.ContainsKey(paramName) ? parameters[paramName] : null;
                var formattedValue = FormatParameterValue(paramValue, paramType);
                paramValues.Add(formattedValue);
            }

            // Формирование SQL-запроса для вызова функции, возвращающей refcursor
            var selectStatement = new StringBuilder();
            selectStatement.Append($"SELECT {functionName}(");
            selectStatement.Append(string.Join(", ", paramValues));
            selectStatement.Append(") AS refcursor;");

            var errors = new List<string>();

            foreach (var role in roles)
            {
                try
                {
                    // Начинаем транзакцию, чтобы курсор сохранялся до выполнения FETCH.
                    await using var transaction = await connection.BeginTransactionAsync();

                    // Установка роли в PostgreSQL
                    using (var roleCommand = connection.CreateCommand())
                    {
                        roleCommand.Transaction = transaction;
                        roleCommand.CommandText = $"SET ROLE \"{role}\";";
                        await roleCommand.ExecuteNonQueryAsync();
                    }

                    // Выполнение функции и получение имени курсора
                    string cursorName = null;
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = selectStatement.ToString();
                        var result = await command.ExecuteScalarAsync();
                        cursorName = result?.ToString();
                        if (string.IsNullOrEmpty(cursorName))
                        {
                            throw new Exception("Получено пустое имя курсора.");
                        }
                    }

                    // Извлечение данных из курсора
                    var rows = new List<Dictionary<string, object>>();
                    using (var fetchCommand = connection.CreateCommand())
                    {
                        fetchCommand.Transaction = transaction;
                        fetchCommand.CommandText = $"FETCH ALL FROM \"{cursorName}\";";
                        using (var reader = await fetchCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    //to:do подумать над типами
                                    object value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                                    // Если значение не является примитивным, попробуем привести к строке.
                                    if (value != null && !(value is string) && !(value.GetType().IsPrimitive))
                                    {
                                        value = value.ToString();
                                    }

                                    row[reader.GetName(i)] = value;
                                }
                                rows.Add(row);
                            }
                        }
                    }

                    // Фиксируем транзакцию
                    await transaction.CommitAsync();

                    var jsonResult = System.Text.Json.JsonSerializer.Serialize(rows);
                    return Content(jsonResult, "application/json");
                }
                catch (Npgsql.PostgresException pgEx)
                {
                    Console.WriteLine($"PostgreSQL ошибка для роли {role}: {pgEx.MessageText}");
                    errors.Add($"Role: {role}, Error: {pgEx.MessageText}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка выполнения для роли {role}: {ex.Message}");
                    errors.Add($"Role: {role}, Error: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Вызов функции завершился неудачно для всех ролей.",
                rolesTried = roles,
                errors = errors
            });
        }
        catch (Npgsql.PostgresException pgEx)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Возникла ошибка PostgreSQL при выполнении функции.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Ошибка при выполнении функции.",
                error = ex.Message
            });
        }
    }


    private async Task<IActionResult> RestProcedure(string procedureName, Dictionary<string, object> parameters, string method)
    {
        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            var roles = GetRolesFromJwtToken();
            if (roles == null || roles.Count == 0)
            {
                return StatusCode(403, new { message = "No roles available to set." });
            }

            var parameterDefinitions = await GetProcedureParametersAsync(procedureName, connection);
            var paramValues = new List<string>();
            foreach (var parameterDefinition in parameterDefinitions)
            {
                var paramName = parameterDefinition.ParameterName;
                var paramType = parameterDefinition.DataType;
                var paramValue = parameters.ContainsKey(paramName) ? parameters[paramName] : null;
                var formattedValue = FormatParameterValue(paramValue, paramType);
                paramValues.Add(formattedValue);
            }

            var callStatement = $"CALL {procedureName}({string.Join(", ", paramValues)});";
            var errors = new List<string>(); // Collect errors for all roles that failed

            foreach (var role in roles)
            {
                try
                {
                    // Set role for the current iteration
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Execute the stored procedure
                    using var command = connection.CreateCommand();
                    command.CommandText = callStatement;
                    await command.ExecuteNonQueryAsync();

                    // If successful, return a JSON object with success message and role
                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure executed successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    // Log PostgreSQL-specific error for the current role and continue to the next one
                    Console.WriteLine($"PostgreSQL error for role {role}: {pgEx.MessageText}");
                    errors.Add($"Role: {role}, Error: {pgEx.MessageText}");
                }
                catch (Exception ex)
                {
                    // Log general error for the current role and continue to the next one
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                    errors.Add($"Role: {role}, Error: {ex.Message}");
                }
            }

            // If no roles succeeded, return a JSON failure message with the errors
            return StatusCode(403, new
            {
                success = false,
                message = "Procedure execution failed for all roles.",
                rolesTried = roles,
                errors = errors
            });
        }
        catch (PostgresException pgEx)
        {
            // Handle PostgreSQL-specific errors
            return StatusCode(500, new
            {
                success = false,
                message = "PostgreSQL error occurred during procedure execution.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            // Handle any other general errors
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing procedure.",
                error = ex.Message
            });
        }
    }









    private string FormatParameterValue(object value, string dataType)
    {
        if (value == null || value is DBNull)
        {
            return "NULL";
        }

        if (value is JsonElement jsonElement)
        {
            // Handle JsonElement types
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return $"'{jsonElement.GetString()}'";
            }
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                return jsonElement.ToString(); // Directly convert numbers to string
            }
            if (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False)
            {
                return jsonElement.GetBoolean() ? "true" : "false";
            }
        }

        return dataType switch
        {
            "integer" => value.ToString(),
            "boolean" => Convert.ToBoolean(value) ? "true" : "false",
            "date" => $"'{Convert.ToDateTime(value).ToString("yyyy-MM-dd")}'",
            "character varying" => $"'{value.ToString().Replace("'", "''")}'", // Escape single quotes in strings
            "text" => $"'{value.ToString().Replace("'", "''")}'",
            "numeric" => value.ToString(),
            "double precision" => value.ToString(),
            "timestamp without time zone" => $"'{Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss")}'",
            _ => value.ToString() // Default case
        };
    }


    private async Task<List<(string ParameterName, string DataType)>> GetProcedureParametersAsync(string procedureName, DbConnection connection)
    {
        var parameters = new List<(string ParameterName, string DataType)>();

        // Разбираем имя схемы и процедуры
        var parts = procedureName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "public";
        var procName = parts.Length == 2 ? parts[1] : parts[0];

        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH param_info AS (
    SELECT 
        unnest(proargnames) AS param_name,
        unnest(proargtypes) AS param_type_oid
    FROM 
        pg_proc
    JOIN 
        pg_namespace ON pg_namespace.oid = pg_proc.pronamespace
    WHERE 
        pg_proc.proname = @procedureName
        AND pg_namespace.nspname = @schemaName
)
SELECT 
    param_name,
    pg_catalog.format_type(param_type_oid, NULL) AS data_type
FROM 
    param_info";

        command.Parameters.Add(new NpgsqlParameter("@procedureName", procName));
        command.Parameters.Add(new NpgsqlParameter("@schemaName", schemaName));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var paramName = reader.GetString(0);
            var dataType = reader.GetString(1);

            if (paramName.StartsWith("p_"))
            {
                paramName = paramName[2..];
            }

            parameters.Add((paramName, dataType));
        }

        return parameters;
    }





    private async Task<IActionResult> ExecuteFunction(string functionName, Dictionary<string, object> parameters, string method)
    {
        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            // Получение ролей из JWT токена
            var roles = GetRolesFromJwtToken();

            // Если роли не найдены, возвращаем ошибку
            if (roles == null || roles.Count == 0)
            {
                return StatusCode(403, new { success = false, message = "No roles available to set." });
            }

            // Получение параметров функции
            var parameterDefinitions = await GetFunctionParametersAsync(functionName, connection);

            // Подготовка параметров для вызова функции
            var paramValues = new List<string>();
            foreach (var parameterDefinition in parameterDefinitions)
            {
                var paramName = parameterDefinition.ParameterName;
                var paramType = parameterDefinition.DataType;
                var paramValue = parameters.ContainsKey(paramName) ? parameters[paramName] : null;

                // Форматирование значения параметра
                var formattedValue = FormatParameterValue(paramValue, paramType);
                paramValues.Add(formattedValue);
            }

            // Формирование запроса SELECT для вызова функции
            var selectStatement = new StringBuilder();
            selectStatement.Append($"SELECT {functionName}(");
            selectStatement.Append(string.Join(", ", paramValues));
            selectStatement.Append(") AS result;");

            var errors = new List<string>(); // To store errors for all roles tried

            foreach (var role in roles)
            {
                try
                {
                    // Установка роли в PostgreSQL
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Выполнение функции и получение результата
                    using var command = connection.CreateCommand();
                    command.CommandText = selectStatement.ToString();
                    var result = await command.ExecuteScalarAsync();

                    // Предполагается, что функция возвращает JSON-строку
                    var jsonResult = result?.ToString();

                    // Возвращаем результат как JSON
                    return Content(jsonResult, "application/json");
                }
                catch (PostgresException pgEx)
                {
                    // Логирование ошибки PostgreSQL для текущей роли и продолжение с другой ролью
                    Console.WriteLine($"PostgreSQL error for role {role}: {pgEx.MessageText}");
                    errors.Add($"Role: {role}, Error: {pgEx.MessageText}");
                }
                catch (Exception ex)
                {
                    // Логирование общей ошибки и продолжение с другой ролью
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                    errors.Add($"Role: {role}, Error: {ex.Message}");
                }
            }

            // Если ни одна роль не сработала, возвращаем сообщение о неудаче с подробными ошибками
            return StatusCode(403, new
            {
                success = false,
                message = "Function execution failed for all roles.",
                rolesTried = roles,
                errors = errors
            });
        }
        catch (PostgresException pgEx)
        {
            // Обработка ошибок PostgreSQL
            return StatusCode(500, new
            {
                success = false,
                message = "PostgreSQL error occurred during function execution.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            // Обработка общих ошибок
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing function.",
                error = ex.Message
            });
        }
    }

    public class ExcelSettings
    {
        // Если не передано, можно использовать "Sheet1" по умолчанию
        public string? SheetName { get; set; } = "Sheet1";

        // Задаем дефолтное значение для начальной позиции
        public ReportStartPosition? ReportStartPosition { get; set; } = new ReportStartPosition();

        // При пустом теле будем выводить только заголовки, поэтому можно оставить пустыми коллекции
        public List<ExcelRangeSettings>? Ranges { get; set; } = new List<ExcelRangeSettings>();
        public List<ExcelCellSettings>? Cells { get; set; } = new List<ExcelCellSettings>();
        public Dictionary<int, double>? ColWidths { get; set; } = new Dictionary<int, double>();
        public Dictionary<int, double>? RowHeights { get; set; } = new Dictionary<int, double>();

        public FreezePaneSettings? FreezePane { get; set; } = new FreezePaneSettings();
    }

    public class ReportStartPosition
    {
        public int Row { get; set; } = 1;
        public int Col { get; set; } = 0;
    }

    // В классах стилей также убираем обязательность, задавая типы как nullable
    public class ExcelRangeSettings
    {
        public int Top { get; set; }
        public int Left { get; set; }
        public int Bottom { get; set; }
        public int Right { get; set; }
        public bool? Merge { get; set; }
        public ExcelStyleSettings? Style { get; set; }
    }

    public class ExcelCellSettings
    {
        public int Row { get; set; }
        public int Col { get; set; }
        /// <summary>
        /// Тип ячейки: 0 – string (по умолчанию), 1 – number, 2 – date (миллисекунды), 3 – formula.
        /// </summary>
        public int Type { get; set; } = 0;
        public object? Value { get; set; }
        public ExcelStyleSettings? Style { get; set; }
    }

    public class ExcelStyleSettings
    {
        public string? Format { get; set; }
        public string? FillColor { get; set; }
        public string? FontColor { get; set; }
        public string? FontName { get; set; }
        public int? FontSize { get; set; }
        public bool? Bold { get; set; }
        public string? HorizontalAlignment { get; set; }  // Например: "left", "center", "right"
        public string? VerticalAlignment { get; set; }    // Например: "top", "center", "bottom"
        public ExcelBorderSettings? Border { get; set; }
    }

    public class ExcelBorderSettings
    {
        public string? Type { get; set; }
        public string? Color { get; set; }
    }

    public class FreezePaneSettings
    {
        public int NLeftColumns { get; set; } = 0;
        public int NTopRows { get; set; } = 0;
    }

    /// <summary>
    /// POST-метод для генерации Excel-файла.
    /// Из URL передаётся имя PostgreSQL-функции (которая обязательно должна возвращать refcursor),
    /// а в теле запроса — JSON с настройками формирования Excel.
    /// Если настройки Excel равны null, то генерируется Excel, где на листе выводятся только заголовки столбцов.
    /// </summary>
    /// <param name="functionName">Имя функции PostgreSQL</param>
    /// <param name="excelSettings">Настройки формирования Excel-файла</param>
    /// <returns>Excel-файл в формате .xlsx</returns>
    [HttpPost("excel/{functionName}")]
    public async Task<IActionResult> functionNameExcelPost(string functionName, [FromBody] ExcelSettings? excelSettings)
    {
        // Если настройки не переданы, создаем экземпляр с дефолтными значениями.
        excelSettings ??= new ExcelSettings();
        return await ExecuteFunctionCursorToExcel(functionName, null, excelSettings);
    }


    /// <summary>
    /// Логика вызова PostgreSQL-функции (возвращающей refcursor) и генерации Excel-файла.
    /// Параметр parameters может быть равен null.
    /// </summary>
    private async Task<IActionResult> ExecuteFunctionCursorToExcel(
        string functionName,
        Dictionary<string, object> parameters,
        ExcelSettings excelSettings)
    {
        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            // Получение ролей из JWT токена – реализуйте по вашей логике.
            var roles = GetRolesFromJwtToken();
            if (roles == null || roles.Count == 0)
            {
                return StatusCode(403, new { success = false, message = "Нет доступных ролей" });
            }

            // Получаем определения параметров функции (например, через INFORMATION_SCHEMA)
            var parameterDefinitions = await GetFunctionParametersAsync(functionName, connection);
            var paramValues = new List<string>();
            foreach (var paramDef in parameterDefinitions)
            {
                var paramName = paramDef.ParameterName;
                var paramType = paramDef.DataType;
                // Если parameters равен null, возвращаем null для всех параметров.
                object paramValue = (parameters != null && parameters.ContainsKey(paramName)) ? parameters[paramName] : null;
                paramValues.Add(FormatParameterValue(paramValue, paramType));
            }

            // Формируем запрос для вызова функции, возвращающей refcursor.
            var sql = new StringBuilder();
            sql.Append($"SELECT {functionName}(");
            sql.Append(string.Join(", ", paramValues));
            sql.Append(") AS refcursor;");

            var errors = new List<string>();

            foreach (var role in roles)
            {
                try
                {
                    // Открываем транзакцию – курсор существует только в рамках транзакции.
                    await using var transaction = await connection.BeginTransactionAsync();

                    // Установка роли
                    using (var roleCommand = connection.CreateCommand())
                    {
                        roleCommand.Transaction = transaction;
                        roleCommand.CommandText = $"SET ROLE \"{role}\";";
                        await roleCommand.ExecuteNonQueryAsync();
                    }

                    // Вызов функции для получения имени курсора
                    string cursorName = null;
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = sql.ToString();
                        var result = await command.ExecuteScalarAsync();
                        cursorName = result?.ToString();
                        if (string.IsNullOrEmpty(cursorName))
                        {
                            throw new Exception("Получено пустое имя курсора.");
                        }
                    }

                    // Извлечение данных из курсора
                    var rows = new List<Dictionary<string, object>>();
                    var columnNames = new List<string>();
                    using (var fetchCommand = connection.CreateCommand())
                    {
                        fetchCommand.Transaction = transaction;
                        fetchCommand.CommandText = $"FETCH ALL FROM \"{cursorName}\";";
                        using (var reader = await fetchCommand.ExecuteReaderAsync())
                        {
                            // Считываем имена колонок
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                columnNames.Add(reader.GetName(i));
                            }

                            // Считываем строки
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    if (value != null && !(value is string) && !value.GetType().IsPrimitive)
                                    {
                                        value = value.ToString();
                                    }
                                    row[columnNames[i]] = value;
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    await transaction.CommitAsync();

                    // Формирование Excel файла через EPPlus
                    using (var package = new ExcelPackage())
                    {
                        // Лист: либо из настроек, либо "Sheet1" по умолчанию
                        string sheetName = excelSettings?.SheetName ?? "Sheet1";
                        var worksheet = package.Workbook.Worksheets.Add(sheetName);

                        // Определяем начальную позицию: ReportStartPosition с учётом 1-индексации EPPlus.
                        int startRow = excelSettings?.ReportStartPosition?.Row ?? 1;
                        int startCol = (excelSettings?.ReportStartPosition?.Col ?? 0) + 1;

                        // Если Excel-настройки отсутствуют, просто выводим заголовки столбцов.
                        for (int i = 0; i < columnNames.Count; i++)
                        {
                            worksheet.Cells[startRow, startCol + i].Value = columnNames[i];
                        }

                        // Записываем данные начиная со строки ниже заголовка.
                        int currentRow = startRow + 1;
                        foreach (var row in rows)
                        {
                            for (int i = 0; i < columnNames.Count; i++)
                            {
                                worksheet.Cells[currentRow, startCol + i].Value = row[columnNames[i]];
                            }
                            currentRow++;
                        }

                        // Если настройки Excel переданы, применяем их для форматирования (диапазоны, ячейки, размеры, фиксация панелей).
                        if (excelSettings != null)
                        {
                            // Применяем настройки диапазонов
                            if (excelSettings.Ranges != null)
                            {
                                foreach (var range in excelSettings.Ranges)
                                {
                                    int top = startRow + range.Top;
                                    int left = startCol + range.Left;
                                    int bottom = startRow + range.Bottom;
                                    int right = startCol + range.Right;
                                    var excelRange = worksheet.Cells[top, left, bottom, right];

                                    if (range.Merge.HasValue && range.Merge.Value)
                                        excelRange.Merge = true;

                                    if (range.Style != null)
                                    {
                                        if (!string.IsNullOrEmpty(range.Style.Format))
                                            excelRange.Style.Numberformat.Format = range.Style.Format;
                                        if (!string.IsNullOrEmpty(range.Style.FillColor))
                                        {
                                            excelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            excelRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + range.Style.FillColor));
                                        }
                                        if (!string.IsNullOrEmpty(range.Style.FontColor))
                                            excelRange.Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + range.Style.FontColor));
                                        if (!string.IsNullOrEmpty(range.Style.FontName))
                                            excelRange.Style.Font.Name = range.Style.FontName;
                                        if (range.Style.FontSize.HasValue)
                                            excelRange.Style.Font.Size = range.Style.FontSize.Value;
                                        if (range.Style.Bold.HasValue)
                                            excelRange.Style.Font.Bold = range.Style.Bold.Value;
                                        if (!string.IsNullOrEmpty(range.Style.HorizontalAlignment))
                                            excelRange.Style.HorizontalAlignment = (ExcelHorizontalAlignment)Enum.Parse(typeof(ExcelHorizontalAlignment), range.Style.HorizontalAlignment, true);
                                        if (!string.IsNullOrEmpty(range.Style.VerticalAlignment))
                                            excelRange.Style.VerticalAlignment = (ExcelVerticalAlignment)Enum.Parse(typeof(ExcelVerticalAlignment), range.Style.VerticalAlignment, true);
                                        if (range.Style.Border != null)
                                        {
                                            var border = excelRange.Style.Border;
                                            border.Top.Style = border.Bottom.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Medium;
                                            if (!string.IsNullOrEmpty(range.Style.Border.Color))
                                            {
                                                var color = System.Drawing.ColorTranslator.FromHtml("#" + range.Style.Border.Color);
                                                border.Top.Color.SetColor(color);
                                                border.Bottom.Color.SetColor(color);
                                                border.Left.Color.SetColor(color);
                                                border.Right.Color.SetColor(color);
                                            }
                                        }
                                    }
                                }
                            }

                            // Применяем настройки отдельных ячеек
                            if (excelSettings.Cells != null)
                            {
                                foreach (var cell in excelSettings.Cells)
                                {
                                    int r = startRow + cell.Row;
                                    int c = startCol + cell.Col;
                                    var wsCell = worksheet.Cells[r, c];
                                    switch (cell.Type)
                                    {
                                        case 1:
                                            if (double.TryParse(cell.Value.ToString(), out double numVal))
                                                wsCell.Value = numVal;
                                            else
                                                wsCell.Value = cell.Value;
                                            break;
                                        case 2:
                                            long ms = Convert.ToInt64(cell.Value);
                                            wsCell.Value = DateTimeOffset.FromUnixTimeMilliseconds(ms).DateTime;
                                            break;
                                        case 3:
                                            wsCell.Formula = cell.Value.ToString();
                                            break;
                                        default:
                                            wsCell.Value = cell.Value;
                                            break;
                                    }
                                    if (cell.Style != null)
                                    {
                                        if (!string.IsNullOrEmpty(cell.Style.Format))
                                            wsCell.Style.Numberformat.Format = cell.Style.Format;
                                        if (!string.IsNullOrEmpty(cell.Style.FillColor))
                                        {
                                            wsCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            wsCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + cell.Style.FillColor));
                                        }
                                        if (!string.IsNullOrEmpty(cell.Style.FontColor))
                                            wsCell.Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#" + cell.Style.FontColor));
                                        if (!string.IsNullOrEmpty(cell.Style.FontName))
                                            wsCell.Style.Font.Name = cell.Style.FontName;
                                        if (cell.Style.FontSize.HasValue)
                                            wsCell.Style.Font.Size = cell.Style.FontSize.Value;
                                        if (cell.Style.Bold.HasValue)
                                            wsCell.Style.Font.Bold = cell.Style.Bold.Value;
                                        if (!string.IsNullOrEmpty(cell.Style.HorizontalAlignment))
                                            wsCell.Style.HorizontalAlignment = (ExcelHorizontalAlignment)Enum.Parse(typeof(ExcelHorizontalAlignment), cell.Style.HorizontalAlignment, true);
                                        if (!string.IsNullOrEmpty(cell.Style.VerticalAlignment))
                                            wsCell.Style.VerticalAlignment = (ExcelVerticalAlignment)Enum.Parse(typeof(ExcelVerticalAlignment), cell.Style.VerticalAlignment, true);
                                        if (cell.Style.Border != null)
                                        {
                                            var border = wsCell.Style.Border;
                                            border.Top.Style = border.Bottom.Style = border.Left.Style = border.Right.Style = ExcelBorderStyle.Medium;
                                            if (!string.IsNullOrEmpty(cell.Style.Border.Color))
                                            {
                                                var color = System.Drawing.ColorTranslator.FromHtml("#" + cell.Style.Border.Color);
                                                border.Top.Color.SetColor(color);
                                                border.Bottom.Color.SetColor(color);
                                                border.Left.Color.SetColor(color);
                                                border.Right.Color.SetColor(color);
                                            }
                                        }
                                    }
                                }
                            }

                            // Настройка ширины столбцов
                            if (excelSettings.ColWidths != null)
                            {
                                foreach (var kvp in excelSettings.ColWidths)
                                {
                                    int colIndex = startCol + kvp.Key;
                                    worksheet.Column(colIndex).Width = kvp.Value;
                                }
                            }

                            // Настройка высоты строк
                            if (excelSettings.RowHeights != null)
                            {
                                foreach (var kvp in excelSettings.RowHeights)
                                {
                                    int rowIndex = startRow + kvp.Key;
                                    worksheet.Row(rowIndex).Height = kvp.Value;
                                }
                            }

                            // Фиксация панелей
                            if (excelSettings.FreezePane != null)
                            {
                                worksheet.View.FreezePanes(excelSettings.FreezePane.NTopRows + 1, excelSettings.FreezePane.NLeftColumns + 1);
                            }
                        } // end if excelSettings != null

                        // Сохраняем Excel-файл в память
                        var stream = new MemoryStream();
                        package.SaveAs(stream);
                        stream.Position = 0;
                        return File(
                            stream,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "report.xlsx");
                    }
                }
                catch (Npgsql.PostgresException pgEx)
                {
                    errors.Add($"Role: {role}, Error: {pgEx.MessageText}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Role: {role}, Error: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Вызов функции завершился неудачно для всех ролей.",
                errors = errors
            });
        }
        catch (Npgsql.PostgresException pgEx)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Ошибка PostgreSQL при выполнении функции.",
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Ошибка при выполнении функции.",
                error = ex.Message
            });
        }
    }


    private async Task<List<(string ParameterName, string DataType)>> GetFunctionParametersAsync(string functionName, DbConnection connection)
    {
        var parameters = new List<(string ParameterName, string DataType)>();

        // Разбираем имя схемы и функции
        var parts = functionName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "public";
        var funcName = parts.Length == 2 ? parts[1] : parts[0];

        using var command = connection.CreateCommand();
        command.CommandText = @"
WITH param_info AS (
    SELECT 
        unnest(proargnames) AS param_name,
        unnest(proargtypes) AS param_type_oid
    FROM 
        pg_proc
    JOIN 
        pg_namespace ON pg_namespace.oid = pg_proc.pronamespace
    WHERE 
        pg_proc.proname = @functionName
        AND pg_namespace.nspname = @schemaName
)
SELECT 
    param_name,
    pg_catalog.format_type(param_type_oid, NULL) AS data_type
FROM 
    param_info";

        command.Parameters.Add(new NpgsqlParameter("@functionName", funcName));
        command.Parameters.Add(new NpgsqlParameter("@schemaName", schemaName));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var paramName = reader.GetString(0);
            var dataType = reader.GetString(1);

            if (paramName.StartsWith("p_"))
            {
                paramName = paramName[2..];
            }

            parameters.Add((paramName, dataType));
        }

        return parameters;
    }


    private List<string> GetRolesFromJwtToken()
    {
        // Extract the token from the Authorization header
        var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Extract the username from the token
        var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

        if (string.IsNullOrEmpty(username))
        {
            return new List<string>();
        }

        // Find the user in the Users table by username
        var user = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                                  .FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            return new List<string>();
        }

        // Retrieve the roles associated with the user
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();

        return roles;
    }


    [HttpPost("uploadFile")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        try
        {
            // Define the path where the file will be saved
            var folderPath = Path.Combine("/var/www/ncatbird.ru/html", "docx");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath); // Create directory if it doesn't exist
            }

            // Generate a unique file name to avoid conflicts
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the file path
            var fileUrl = $"{Request.Scheme}://{Request.Host}/docx/{fileName}";
            return Ok(new { filePath = fileUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }


}


