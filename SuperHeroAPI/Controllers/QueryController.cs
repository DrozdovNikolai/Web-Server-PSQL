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


   



    [HttpPost]



    public async Task<IActionResult> Post([FromBody] QueryRequest queryRequest)
    {
        var userRole = User.FindFirstValue(ClaimTypes.Role);

        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Query))
        {
            return BadRequest("Query is missing or empty.");
        }

        try
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                // Set the role based on the user's role received from the claim
                command.CommandText = $"SET ROLE {userRole}; {queryRequest.Query}";
                command.CommandType = CommandType.Text;
                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Check if the result set contains any rows
                    if (!reader.HasRows)
                    {
                        return Ok("Пустой вывод."); // Return 404 Not Found if no rows are returned
                    }

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

                    // If only one row is returned, return it as a single object, not an array
                    if (result.Count == 1)
                    {


                        return Ok(result[0]);

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
        return await ExecuteFunction2($"select_{tableName}", parameters, "GET");
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
        return await ExecuteFunction2($"{functionName}", parameters, "GET");
    }

    [HttpPost("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePost(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction2($"{functionName}", parameters, "POST");
    }

    [HttpPut("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePut(string functionName, [FromBody] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction2($"{functionName}", parameters, "PUT");
    }

    [HttpDelete("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNameDelete(string functionName, [FromQuery] Dictionary<string, object> parameters)
    {
        return await ExecuteFunction2($"{functionName}", parameters, "DELETE");
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
                Console.WriteLine(parameters);
                var paramValue = parameters.ContainsKey(paramName) ? parameters[paramName] : null;
                var formattedValue = FormatParameterValue(paramValue, paramType);
                paramValues.Add(formattedValue);
            }

            var callStatement = $"CALL {procedureName}({string.Join(", ", paramValues)});";

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = callStatement;
                    await command.ExecuteNonQueryAsync();

                    // Return a JSON object with success message and role
                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure executed successfully for role {role}.",
                        role = role
                    });
                }
                catch (Exception ex)
                {
                    // Log or handle exception, then continue to the next role
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                    // The loop naturally continues here
                }
            }

            // If no roles succeeded, return a JSON failure message
            return StatusCode(403, new
            {
                success = false,
                message = "Procedure execution failed for all roles.",
                rolesTried = roles
            });
        }
        catch (PostgresException pgEx)
        {
            // Handle PostgreSQL-specific errors
            return StatusCode(500, new
            {
                success = false,
                postgresError = pgEx.MessageText,
                postgresDetails = pgEx.Detail,
                postgresHint = pgEx.Hint,
                postgresCode = pgEx.SqlState
            }); ;
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
            AND pg_namespace.nspname = 'public'
    )
    SELECT 
        param_name,
        pg_catalog.format_type(param_type_oid, NULL) AS data_type
    FROM 
        param_info";
        command.Parameters.Add(new NpgsqlParameter("@procedureName", procedureName));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var paramName = reader.GetString(0);
            var dataType = reader.GetString(1);

            // Remove 'p_' prefix if present
            if (paramName.StartsWith("p_"))
            {
                paramName = paramName.Substring(2); // Remove the first two characters
            }

            parameters.Add((paramName, dataType));
        }

        return parameters;
    }











    private async Task<IActionResult> ExecuteFunction2(string functionName, Dictionary<string, object> parameters, string method)
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
                catch (Exception ex)
                {
                    // Логирование ошибки и продолжение с другой ролью
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                    // Переход к следующей роли
                }
            }

            // Если ни одна роль не сработала, возвращаем сообщение о неудаче
            return StatusCode(403, new
            {
                success = false,
                message = "Function execution failed for all roles.",
                rolesTried = roles
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



    private async Task<List<(string ParameterName, string DataType)>> GetFunctionParametersAsync(string functionName, DbConnection connection)
    {
        var parameters = new List<(string ParameterName, string DataType)>();

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
            AND pg_namespace.nspname = 'public'
    )
    SELECT 
        param_name,
        pg_catalog.format_type(param_type_oid, NULL) AS data_type
    FROM 
        param_info";
        command.Parameters.Add(new NpgsqlParameter("@functionName", functionName));

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var paramName = reader.GetString(0);
            var dataType = reader.GetString(1);

            // Remove 'p_' prefix if present
            if (paramName.StartsWith("p_"))
            {
                paramName = paramName.Substring(2); // Remove the first two characters
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


    public class ProcedureRequest
    {
        public string ProcedureName { get; set; }
        public List<ProcedureParameter> Parameters { get; set; }
        public string Body { get; set; } // SQL logic
    }

    public class ProcedureParameter
    {
        public string ParameterName { get; set; }
        public string DataType { get; set; }
    }
    public class FunctionRequest
    {
        public string FunctionName { get; set; }
        public List<ProcedureParameter> Parameters { get; set; }
        public string Body { get; set; } // SQL logic for the function
        public string ReturnType { get; set; } // Return type of the function (e.g., integer, text, json, etc.)
    }
 
    [HttpPost("CreateProcedureFromSql")]
    public async Task<IActionResult> CreateProcedureFromSql([FromBody] string sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return BadRequest("SQL code is required.");
        }

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    var procedureName = ExtractProcedureNameFromSql(sql);
                    if (string.IsNullOrEmpty(procedureName))
                    {
                        return BadRequest("Unable to extract procedure name from the SQL code.");
                    }

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER PROCEDURE {procedureName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
              

                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

                    // Add new row to ProcedureUser table
                    var procedureUser = new ProcedureUser
                    {
                        ProcedureName = procedureName, // Corrected to use the variable
                        UserId = user.Id
                    };

                    _context.ProcedureUsers.Add(procedureUser);
                    await _context.SaveChangesAsync(); // Save changes to the database

                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure created successfully with given SQL for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error creating procedure.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Procedure creation failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing procedure creation SQL.",
                error = ex.Message
            });
        }
    }





    [HttpPut("UpdateProcedureFromSql")]
    public async Task<IActionResult> UpdateProcedureFromSql([FromBody] string sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return BadRequest("SQL code is required.");
        }

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    var procedureName = ExtractProcedureNameFromSql(sql);
                    if (string.IsNullOrEmpty(procedureName))
                    {
                        return BadRequest("Unable to extract procedure name from the SQL code.");
                    }

                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }

                    using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = $"DROP PROCEDURE IF EXISTS {procedureName};";
                    await dropCommand.ExecuteNonQueryAsync();

                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER PROCEDURE {procedureName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();

                    // Get the username from the JWT token
                 

                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

                    // Remove the old ProcedureUser row if it exists
                    var existingProcedureUser = await _context.ProcedureUsers
                        .FirstOrDefaultAsync(pu => pu.ProcedureName == procedureName && pu.UserId == user.Id);

                    if (existingProcedureUser != null)
                    {
                        _context.ProcedureUsers.Remove(existingProcedureUser);
                    }

                    // Add new ProcedureUser row
                    var procedureUser = new ProcedureUser
                    {
                        ProcedureName = procedureName,
                        UserId = user.Id
                    };

                    _context.ProcedureUsers.Add(procedureUser);
                    await _context.SaveChangesAsync(); // Save changes to the database

                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure updated successfully with given SQL for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error updating procedure.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Procedure update failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing procedure update SQL.",
                error = ex.Message
            });
        }
    }
    [HttpDelete("DeleteProcedure/{procedureName}")]
    public async Task<IActionResult> DeleteProcedure(string procedureName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = $"DROP PROCEDURE IF EXISTS {procedureName};";
                    await command.ExecuteNonQueryAsync();
                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER PROCEDURE {procedureName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
              

                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

                    // Remove the corresponding ProcedureUser row
                    var existingProcedureUser = await _context.ProcedureUsers
                        .FirstOrDefaultAsync(pu => pu.ProcedureName == procedureName && pu.UserId == user.Id);

                    if (existingProcedureUser != null)
                    {
                        _context.ProcedureUsers.Remove(existingProcedureUser);
                        await _context.SaveChangesAsync(); // Save changes to the database
                    }

                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure '{procedureName}' deleted successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error deleting procedure.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Delete failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error deleting procedure.",
                error = ex.Message
            });
        }
    }
    [HttpPost("CreateFunctionFromSql")]
    public async Task<IActionResult> CreateFunctionFromSql([FromBody] string sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return BadRequest("SQL code is required.");
        }

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
     
            await connection.OpenAsync();
 

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    var functionName = ExtractFunctionNameFromSql(sql);
                    if (string.IsNullOrEmpty(functionName))
                    {
                        return BadRequest("Unable to extract function name from the SQL code.");
                    }

                    // Execute the function creation SQL
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();

                    string username = User.Identity.Name;
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER FUNCTION {functionName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
                    
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }
                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }
                    var functionUser = new FunctionUser
                    {
                        FunctionName = functionName,
                        UserId = user.Id
                    };

                    _context.FunctionUsers.Add(functionUser);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Function created successfully and ownership set to {username} for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error creating function.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Function creation failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing function creation SQL.",
                error = ex.Message
            });
        }
    }


    [HttpPut("UpdateFunctionFromSql")]
    public async Task<IActionResult> UpdateFunctionFromSql([FromBody] string sql)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return BadRequest("SQL code is required.");
        }

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    var functionName = ExtractFunctionNameFromSql(sql);
                    if (string.IsNullOrEmpty(functionName))
                    {
                        return BadRequest("Unable to extract function name from the SQL code.");
                    }

                    // Execute the SQL to drop the function if it exists
                    using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = $"DROP FUNCTION IF EXISTS {functionName};";
                    await dropCommand.ExecuteNonQueryAsync();

                    // Execute the dynamic function creation SQL (for update)
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();

                    // Get the username from the JWT token
                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
                    // Set the function's owner to the user's username
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER FUNCTION {functionName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Function updated successfully and ownership set to {username} for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error updating function.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Function update failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing function update SQL.",
                error = ex.Message
            });
        }
    }

    [HttpDelete("DeleteFunction/{functionName}")]
    public async Task<IActionResult> DeleteFunction(string functionName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = $"DROP FUNCTION IF EXISTS {functionName};";
                    await command.ExecuteNonQueryAsync();

                    // Get the username from the JWT token
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }

                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

                    // Remove the corresponding FunctionUser row
                    var existingFunctionUser = await _context.FunctionUsers
                        .FirstOrDefaultAsync(fu => fu.FunctionName == functionName && fu.UserId == user.Id);

                    if (existingFunctionUser != null)
                    {
                        _context.FunctionUsers.Remove(existingFunctionUser);
                        await _context.SaveChangesAsync(); // Save changes to the database
                    }

                    return Ok(new
                    {
                        success = true,
                        message = $"Function '{functionName}' deleted successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error deleting function.",
                        postgresError = pgEx.MessageText,
                        postgresDetails = pgEx.Detail,
                        postgresHint = pgEx.Hint,
                        postgresCode = pgEx.SqlState
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new
            {
                success = false,
                message = "Delete failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error deleting function.",
                error = ex.Message
            });
        }
    }
    private string ExtractProcedureNameFromSql(string sql)
    {
        var match = Regex.Match(sql, @"CREATE\s+OR\s+REPLACE\s+PROCEDURE\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private string ExtractFunctionNameFromSql(string sql)
    {
        var match = Regex.Match(sql, @"CREATE\s+OR\s+REPLACE\s+FUNCTION\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }


   
    [HttpGet("GetProcedureInfo/{procedureName}")]
    public async Task<IActionResult> GetProcedureInfo(string procedureName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Query PostgreSQL to get procedure information
                    using var command = connection.CreateCommand();
                    command.CommandText = $@"
                    SELECT 
                        p.proname AS procedure_name,
                        n.nspname AS schema_name,
                        pg_catalog.pg_get_functiondef(p.oid) AS definition
                    FROM 
                        pg_proc p
                    JOIN 
                        pg_namespace n ON p.pronamespace = n.oid
                    WHERE 
                        p.proname = @procedureName
                        AND n.nspname = 'public';"; // or your specific schema

                    command.Parameters.Add(new NpgsqlParameter("@procedureName", procedureName));

                    using var reader = await command.ExecuteReaderAsync();
                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                    {
                        { "ProcedureName", reader["procedure_name"] },
                        { "SchemaName", reader["schema_name"] },
                        { "Definition", reader["definition"] }
                    };
                        result.Add(row);
                    }

                    if (result.Count == 0)
                    {
                        return NotFound($"Procedure '{procedureName}' not found.");
                    }

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    // Log or handle exception, continue to the next role
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to get procedure info for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving procedure info.", error = ex.Message });
        }
    }
    [HttpPut("UpdateProcedure")]
    public async Task<IActionResult> UpdateProcedure([FromBody] ProcedureRequest request)
    {
        if (string.IsNullOrEmpty(request.ProcedureName))
        {
            return BadRequest("Procedure name is required.");
        }

        if (request.Parameters == null || !request.Parameters.Any())
        {
            return BadRequest("At least one parameter is required.");
        }

        if (string.IsNullOrEmpty(request.Body))
        {
            return BadRequest("Procedure body is required.");
        }

        // Build the CREATE PROCEDURE statement
        var parameterDefinitions = string.Join(", ", request.Parameters.Select(p => $"{p.ParameterName} {p.DataType}"));
        string createProcedureSql = $@"
        CREATE PROCEDURE {request.ProcedureName}({parameterDefinitions})
        LANGUAGE plpgsql AS $$
        BEGIN
            {request.Body}
        END;
        $$;";

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Delete the procedure if it exists
                    using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = $"DROP PROCEDURE IF EXISTS {request.ProcedureName};";
                    await dropCommand.ExecuteNonQueryAsync();

                    // Create the new procedure
                    using var createCommand = connection.CreateCommand();
                    createCommand.CommandText = createProcedureSql;
                    await createCommand.ExecuteNonQueryAsync();

                    // Return a success message with the role
                    return Ok(new
                    {
                        success = true,
                        message = $"Procedure '{request.ProcedureName}' updated successfully for role {role}.",
                        role = role
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Update failed for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error updating procedure.", error = ex.Message });
        }
    }

   



    [HttpGet("GetFunctionInfo/{functionName}")]
    public async Task<IActionResult> GetFunctionInfo(string functionName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = $@"
                    SELECT 
                        p.proname AS function_name,
                        n.nspname AS schema_name,
                        pg_catalog.pg_get_functiondef(p.oid) AS definition
                    FROM 
                        pg_proc p
                    JOIN 
                        pg_namespace n ON p.pronamespace = n.oid
                    WHERE 
                        p.proname = @functionName
                        AND n.nspname = 'public';";

                    command.Parameters.Add(new NpgsqlParameter("@functionName", functionName));

                    using var reader = await command.ExecuteReaderAsync();
                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                    {
                        { "FunctionName", reader["function_name"] },
                        { "SchemaName", reader["schema_name"] },
                        { "Definition", reader["definition"] }
                    };
                        result.Add(row);
                    }

                    if (result.Count == 0)
                    {
                        return NotFound($"Function '{functionName}' not found.");
                    }

                    return Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to get function info for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving function info.", error = ex.Message });
        }
    }


    
  

   

    [HttpGet("GetAllProcedures")]
    public async Task<IActionResult> GetAllProcedures()
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Query for all procedures
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT p.proname AS procedure_name,
                           n.nspname AS schema_name
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE p.prokind = 'p' AND n.nspname = 'public';"; // 'p' denotes procedures

                    using var reader = await command.ExecuteReaderAsync();
                    var procedures = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var procedure = new Dictionary<string, object>
                    {
                        { "ProcedureName", reader["procedure_name"] },
                        { "SchemaName", reader["schema_name"] }
                    };
                        procedures.Add(procedure);
                    }

                    return Ok(procedures);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to retrieve procedures for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving procedures.", error = ex.Message });
        }
    }
    [HttpGet("GetAllFunctions")]
    public async Task<IActionResult> GetAllFunctions()
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Query for all functions
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT p.proname AS function_name,
                           n.nspname AS schema_name
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE p.prokind = 'f' AND n.nspname = 'public';"; // 'f' denotes functions

                    using var reader = await command.ExecuteReaderAsync();
                    var functions = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var function = new Dictionary<string, object>
                    {
                        { "FunctionName", reader["function_name"] },
                        { "SchemaName", reader["schema_name"] }
                    };
                        functions.Add(function);
                    }

                    return Ok(functions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to retrieve functions for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving functions.", error = ex.Message });
        }
    }
    [HttpGet("GetProcedureSqlScript/{procedureName}")]
    public async Task<IActionResult> GetProcedureSqlScript(string procedureName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Query to get the SQL script for the procedure
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT pg_get_functiondef(p.oid) AS definition
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE p.proname = @procedureName
                    AND p.prokind = 'p' AND n.nspname = 'public';"; // 'p' denotes procedures

                    command.Parameters.Add(new NpgsqlParameter("@procedureName", procedureName));

                    var sqlScript = await command.ExecuteScalarAsync();
                    if (sqlScript == null)
                    {
                        return NotFound($"Procedure '{procedureName}' not found.");
                    }

                    return Ok(new { ProcedureName = procedureName, SqlScript = sqlScript.ToString() });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to retrieve procedure for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving procedure SQL script.", error = ex.Message });
        }
    }
    [HttpGet("GetFunctionSqlScript/{functionName}")]
    public async Task<IActionResult> GetFunctionSqlScript(string functionName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        using var connection = _context.Database.GetDbConnection();
        try
        {
            await connection.OpenAsync();

            foreach (var role in roles)
            {
                try
                {
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Query to get the SQL script for the function
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                    SELECT pg_get_functiondef(p.oid) AS definition
                    FROM pg_proc p
                    JOIN pg_namespace n ON p.pronamespace = n.oid
                    WHERE p.proname = @functionName
                    AND p.prokind = 'f' AND n.nspname = 'public';"; // 'f' denotes functions

                    command.Parameters.Add(new NpgsqlParameter("@functionName", functionName));

                    var sqlScript = await command.ExecuteScalarAsync();
                    if (sqlScript == null)
                    {
                        return NotFound($"Function '{functionName}' not found.");
                    }

                    return Ok(new { FunctionName = functionName, SqlScript = sqlScript.ToString() });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Execution failed for role {role}: {ex.Message}");
                }
            }

            return StatusCode(403, new { message = "Unable to retrieve function for all roles." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Error retrieving function SQL script.", error = ex.Message });
        }
    }

}


