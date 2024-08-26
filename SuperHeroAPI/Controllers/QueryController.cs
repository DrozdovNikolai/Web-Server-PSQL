using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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


    // Method to create stored procedures for a given table name
    [HttpPost("CreateDynamicProcedures")]
    public async Task<IActionResult> CreateDynamicProcedures([FromBody] string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
        {
            return BadRequest("Table name is required.");
        }

        // Create procedure to insert a new record
        string createInsertProcedureSql = $@"
        CREATE OR REPLACE PROCEDURE Insert_{tableName}(
            {string.Join(", ", GetColumnDefinitionsForTable2(tableName))}
        )
        LANGUAGE plpgsql AS $$
        BEGIN
            INSERT INTO {tableName} ({string.Join(", ", GetColumnNamesForTable2(tableName))})
            VALUES ({string.Join(", ", GetColumnNamesForTable2(tableName))});
        END;
        $$;";

        // Create procedure to update a record by ID (assuming ID column is present)
        string createUpdateProcedureSql = $@"
        CREATE OR REPLACE PROCEDURE Update_{tableName}(
            {string.Join(", ", GetColumnDefinitionsForTable2(tableName))}
        )
        LANGUAGE plpgsql AS $$
        BEGIN
            UPDATE {tableName}
            SET {string.Join(", ", GetColumnNamesForTable2(tableName).Select(c => $"{c} = {c}"))}
            WHERE Id = {GetIdColumnNameForTable2(tableName)};
        END;
        $$;";

        // Create procedure to delete a record by ID (assuming ID column is present)
        string createDeleteProcedureSql = $@"
        CREATE OR REPLACE PROCEDURE Delete_{tableName}(
            id INTEGER
        )
        LANGUAGE plpgsql AS $$
        BEGIN
            DELETE FROM {tableName} WHERE Id = id;
        END;
        $$;";

        // Create procedure to select data from the table


        // Create procedure to select data from the table
        string createSelectProcedureSql = $@"
CREATE OR REPLACE FUNCTION Select_{tableName}()
RETURNS JSON AS $$
BEGIN
    RETURN (SELECT json_agg(row_to_json(t)) FROM (SELECT * FROM {tableName}) t);
END;
$$ LANGUAGE plpgsql;
";

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

    private List<string> GetColumnDefinitionsForTable2(string tableName)
    {
        // Execute a SQL query to get column definitions
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

    // Helper method to get column names for a table
    private List<string> GetColumnNamesForTable2(string tableName)
    {
        // Execute a SQL query to get column names
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

    // Helper method to get the ID column name for a table
    private string GetIdColumnNameForTable2(string tableName)
    {
        // Execute a SQL query to get the ID column name
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}' AND column_name = 'Id'";
        command.CommandType = CommandType.Text;

        _context.Database.OpenConnection();
        using var reader = command.ExecuteReader();

        string idColumnName = null;
        if (reader.Read())
        {
            idColumnName = reader.GetString(0);
        }

        _context.Database.CloseConnection();

        return idColumnName ?? "Id"; // Default to "Id" if not found
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
     [HttpGet("ExecuteProcedure/{procedureName}")]
        public async Task<IActionResult> ExecuteProcedureGet(string procedureName, [FromQuery] Dictionary<string, string> parameters)
        {
            return await ExecuteProcedure(procedureName, parameters, "GET");
        }

    
        [HttpPost("ExecuteProcedure/{procedureName}")]
        public async Task<IActionResult> ExecuteProcedurePost(string procedureName, [FromBody] Dictionary<string, string> parameters)
        {
            return await ExecuteProcedure(procedureName, parameters, "POST");
        }

       
        [HttpPut("ExecuteProcedure/{procedureName}")]
        public async Task<IActionResult> ExecuteProcedurePut(string procedureName, [FromBody] Dictionary<string, string> parameters)
        {
            return await ExecuteProcedure(procedureName, parameters, "PUT");
        }

        
        [HttpDelete("ExecuteProcedure/{procedureName}")]
        public async Task<IActionResult> ExecuteProcedureDelete(string procedureName, [FromQuery] Dictionary<string, string> parameters)
        {
            return await ExecuteProcedure(procedureName, parameters, "DELETE");
        }







    private async Task<IActionResult> ExecuteProcedure(string procedureName, Dictionary<string, string> parameters, string httpMethod)
    {
        if (string.IsNullOrEmpty(procedureName))
        {
            return BadRequest("Procedure name is required.");
        }
       

 
        using var connection = _context.Database.GetDbConnection();
        using var command = connection.CreateCommand();

    
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = procedureName;

      
        string tableName = procedureName.Replace("insert_", "").Replace("update_", "").Replace("delete_", "");
        var columnDefinitions = GetColumnDefinitionsForTable(tableName);


        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                var cmdParameter = command.CreateParameter();
                cmdParameter.ParameterName = parameter.Key;

                // Find the column definition for the parameter
                var columnDefinition = columnDefinitions.FirstOrDefault(cd => cd.ColumnName == parameter.Key);
                if (columnDefinition != null)
                {
                    cmdParameter.Value = ConvertToDbType(parameter.Value, columnDefinition.DataType);
                    cmdParameter.DbType = columnDefinition.DbType;
                }
                else
                {
             
                    return BadRequest($"Parameter '{parameter.Key}' not found in table '{tableName}'");
                }

                command.Parameters.Add(cmdParameter);
            }
        }

        try
        {
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            await connection.CloseAsync();

  
            return Ok($"Procedure '{procedureName}' executed successfully using {httpMethod} method.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error executing procedure '{procedureName}': {ex.Message}");
        }
    }
    [HttpGet("rest/{tableName}")]
    public async Task<IActionResult> RestProcedureGet(string tableName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await ExecuteFunction($"Select_{tableName}", parameters, "GET");
    }

    [HttpPost("rest/{tableName}")]
    public async Task<IActionResult> RestProcedurePost(string tableName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"Insert_{tableName}", parameters, "POST");
    }

    [HttpPut("rest/{tableName}")]
    public async Task<IActionResult> RestProcedurePut(string tableName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"Update_{tableName}", parameters, "PUT");
    }

    [HttpDelete("rest/{tableName}")]
    public async Task<IActionResult> RestProcedureDelete(string tableName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"Delete_{tableName}", parameters, "DELETE");
    }



    [HttpGet("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNameGet(string procedureName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await ExecuteProcedure($"{procedureName}", parameters, "GET");
    }

    [HttpPost("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNamePost(string procedureName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "POST");
    }

    [HttpPut("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNamePut(string procedureName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "PUT");
    }

    [HttpDelete("anyProcedure/{procedureName}")]
    public async Task<IActionResult> procedureNameDelete(string procedureName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{procedureName}", parameters, "DELETE");
    }


    [HttpGet("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNameGet(string functionName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await ExecuteProcedure($"{functionName}", parameters, "GET");
    }

    [HttpPost("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePost(string functionName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{functionName}", parameters, "POST");
    }

    [HttpPut("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNamePut(string functionName, [FromBody] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{functionName}", parameters, "PUT");
    }

    [HttpDelete("anyFunction/{functionName}")]
    public async Task<IActionResult> functionNameDelete(string functionName, [FromQuery] Dictionary<string, string> parameters)
    {
        return await RestProcedure($"{functionName}", parameters, "DELETE");
    }

    private async Task<IActionResult> RestProcedure(string procedureName, Dictionary<string, string> parameters, string httpMethod)
    {
        if (string.IsNullOrEmpty(procedureName))
        {
            return BadRequest("Procedure name is required.");
        }

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        var roles = GetRolesFromJwtToken();

        // If no roles are available, return an error
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, "No roles available to set.");
        }

        foreach (var role in roles)
        {
            using var command = connection.CreateCommand();
            try
            {
                // Set the role in PostgreSQL
                command.CommandText = $"SET ROLE {role};";
                await command.ExecuteNonQueryAsync();

                // Proceed with setting up the command to execute the stored procedure
                bool isSelectProcedure = procedureName.StartsWith("select_", StringComparison.OrdinalIgnoreCase);

                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procedureName;

                string tableName = procedureName.Replace("insert_", "").Replace("update_", "").Replace("delete_", "").Replace("select_", "");
                var columnDefinitions = GetColumnDefinitionsForTable(tableName);

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        var cmdParameter = command.CreateParameter();
                        cmdParameter.ParameterName = parameter.Key;

                        // Find the column definition for the parameter
                        var columnDefinition = columnDefinitions.FirstOrDefault(cd => cd.ColumnName == parameter.Key);
                        if (columnDefinition != null)
                        {
                            cmdParameter.Value = ConvertToDbType(parameter.Value, columnDefinition.DataType);
                            cmdParameter.DbType = columnDefinition.DbType;
                        }
                        else
                        {
                            return BadRequest($"Parameter '{parameter.Key}' not found in table '{tableName}'");
                        }

                        command.Parameters.Add(cmdParameter);
                    }
                }

                // Execute the command
                if (isSelectProcedure)
                {
                    using var reader = await command.ExecuteReaderAsync();
                    var result = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                    return Ok(result);
                }
                else
                {
                    await command.ExecuteNonQueryAsync();
                    return Ok($"Procedure '{procedureName}' executed successfully using {httpMethod} method.");
                }
            }
            catch (Exception ex)
            {
                // Log the error for this role, but try the next one
                // Optionally, you could log the role and exception message for further analysis
            }
            finally
            {
                // Ensure the connection is open before resetting the role
                if (connection.State == ConnectionState.Open)
                {
                    try
                    {
                        command.CommandText = "RESET ROLE;";
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception resetEx)
                    {
                        // Handle any exceptions during RESET ROLE, such as logging
                    }
                }
            }
        }

        // If none of the roles work, return an error
        return StatusCode(403, "None of the roles were able to execute the procedure.");
    }
    /*
    private List<string> GetRolesFromJwtToken()
    {
        var token = _httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var rolesClaim = jwtToken.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        if (rolesClaim != null)
        {
            return rolesClaim.Select(c => c.Value).ToList();
        }

        return new List<string>();
    }
    */

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

    private async Task<IActionResult> ExecuteFunction(string functionName, Dictionary<string, string> parameters, string httpMethod)
    {
        if (string.IsNullOrEmpty(functionName))
        {
            return BadRequest("Function name is required.");
        }

        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        var roles = GetRolesFromJwtToken();

        // If no roles are available, return an error
        if (roles == null || roles.Count == 0)
        {
            return StatusCode(403, "No roles available to set.");
        }

        foreach (var role in roles)
        {
            using var command = connection.CreateCommand();
            try
            {
                // Set the role in PostgreSQL
                command.CommandText = $"SET ROLE {role};";
                await command.ExecuteNonQueryAsync();

                // Prepare the function execution command
                command.CommandType = CommandType.Text;
                command.CommandText = $"SELECT {functionName}()";

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        var cmdParameter = command.CreateParameter();
                        cmdParameter.ParameterName = parameter.Key;
                        cmdParameter.Value = parameter.Value;
                        command.Parameters.Add(cmdParameter);
                    }
                }

                var result = "";
                if (httpMethod == "GET")
                {
                    result = (string)await command.ExecuteScalarAsync();
                    return Content(result, "application/json");
                }
                else
                {
                    await command.ExecuteNonQueryAsync();
                    return Ok($"Function '{functionName}' executed successfully using {httpMethod} method.");
                }
            }
            catch (Exception ex)
            {
                // Log the error for this role, but try the next one
                // Optionally, you could log the role and exception message for further analysis
            }
            finally
            {
                // Ensure the connection is open before resetting the role
                if (connection.State == ConnectionState.Open)
                {
                    try
                    {
                        command.CommandText = "RESET ROLE;";
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (Exception resetEx)
                    {
                        // Handle any exceptions during RESET ROLE, such as logging
                    }
                }
            }
        }

        // If none of the roles work, return an error
        return StatusCode(403, "None of the roles were able to execute the function.");
    }


    private bool HasPermission(string procedureName, List<string> userRoles)
    {
        var operation = GetOperationFromProcedureName(procedureName);

        // Get permissions for the user's roles using a JOIN
        var permissions = from p in _context.Permissions
                          join r in _context.Roles on p.RoleId equals r.RoleId
                          where userRoles.Contains(r.RoleName) &&
                                p.TableName == GetTableNameFromProcedureName(procedureName) &&
                                p.Operation == operation
                          select p;

        // Check if any permission matches
        return permissions.Any();
    }

    // Helper method to get the operation from the procedure name
    private CrudOperation GetOperationFromProcedureName(string procedureName)
    {
        switch (procedureName.Split('_')[0])
        {
            case "insert":
                return CrudOperation.Create;
            case "update":
                return CrudOperation.Update;
            case "delete":
                return CrudOperation.Delete;
            default: // Assume Read for other procedures
                return CrudOperation.Read;
        }
    }

    // Helper method to get the table name from the procedure name
    private string GetTableNameFromProcedureName(string procedureName)
    {
        return procedureName.Split('_')[1];
    }

    private object ConvertToDbType(string value, string postgresqlDataType)
    {
        switch (postgresqlDataType.ToLower())
        {
            case "integer":
            case "serial":
                return int.Parse(value);
            case "varchar":
            case "text":
            case "character varying":
                return value;
            case "numeric":
                return decimal.Parse(value);
            case "date":
                return DateTime.Parse(value);
            case "boolean": // Add case for boolean
                return bool.Parse(value);
            default:
                throw new ArgumentException($"Unsupported data type: {postgresqlDataType}");
        }
    }


    private List<ColumnDefinition> GetColumnDefinitionsForTable(string tableName)
    {

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{tableName}'";
        command.CommandType = CommandType.Text;

        _context.Database.OpenConnection();
        using var reader = command.ExecuteReader();

        List<ColumnDefinition> columnDefinitions = new List<ColumnDefinition>();
        while (reader.Read())
        {
            string columnName = reader.GetString(0);
            string dataType = reader.GetString(1);
            columnDefinitions.Add(new ColumnDefinition { ColumnName = columnName, DataType = dataType });
        }

        _context.Database.CloseConnection();

        return columnDefinitions;
    }

  
}


