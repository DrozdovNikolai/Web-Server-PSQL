using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    CREATE OR REPLACE PROCEDURE Insert_{tableName}(
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
    CREATE OR REPLACE PROCEDURE Update_{tableName}(
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
    CREATE OR REPLACE PROCEDURE Delete_{tableName}(
        p_{primaryKeyColumn} INTEGER
    )
    LANGUAGE plpgsql AS $$
    BEGIN
        UPDATE {tableName} SET deleted_at = now() WHERE {primaryKeyColumn} = p_{primaryKeyColumn};
    END;
    $$;";

        // Create procedure to select data from the table
        string createSelectProcedureSql = $@"
    CREATE OR REPLACE FUNCTION Select_{tableName}()
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
        var procedureNames = new[] { "Insert", "Update", "Delete", "Select" };
        foreach (var procedureName in procedureNames)
        {
            var procedureUser = new ProcedureUser
            {
                ProcedureName = $"{procedureName}_{tableName}",
                UserId = user.Id
            };

            _context.ProcedureUsers.Add(procedureUser);
        }

        try
        {
            await _context.SaveChangesAsync();
            return Ok($"Stored procedures for {tableName} table created successfully and entries added to 'procedure_user'.");
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
        catch (Exception ex)
        {
            // Return a JSON object with error details
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

            // Get roles from the JWT token
            var roles = GetRolesFromJwtToken();

            // If no roles are available, return an error in JSON format
            if (roles == null || roles.Count == 0)
            {
                return StatusCode(403, new { success = false, message = "No roles available to set." });
            }

            // Get parameter information
            var parameterDefinitions = await GetFunctionParametersAsync(functionName, connection);

            // Build the parameter values
            var paramValues = new List<string>();
            foreach (var parameterDefinition in parameterDefinitions)
            {
                var paramName = parameterDefinition.ParameterName;
                var paramType = parameterDefinition.DataType;
                var paramValue = parameters.ContainsKey(paramName) ? parameters[paramName] : null;

                // Convert the parameter value to a string representation
                var formattedValue = FormatParameterValue(paramValue, paramType);
                paramValues.Add(formattedValue);
            }

            // Prepare the SELECT statement with the function call
            var selectStatement = new StringBuilder();
            selectStatement.Append($"SELECT {functionName}(");
            selectStatement.Append(string.Join(", ", paramValues));
            selectStatement.Append(") AS result;");

            foreach (var role in roles)
            {
                try
                {
                    // Set the role in PostgreSQL
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Execute the function and get the JSON result
                    using var command = connection.CreateCommand();
                    command.CommandText = selectStatement.ToString();
                    var result = await command.ExecuteScalarAsync();

                    // Assuming the function returns a JSON string, cast the result to string
                    var jsonResult = result?.ToString();

                    // Return the result directly as JSON content
                    return Content(jsonResult, "application/json");
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
                message = "Function execution failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            // Return a JSON object with error details
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




}


