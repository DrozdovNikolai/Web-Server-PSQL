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
public class ProcedureListController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IRoleService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProcedureListController(DataContext context, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _roleService = roleService;
        _httpContextAccessor = httpContextAccessor;
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


    private string ExtractProcedureNameFromSql(string sql)
    {
        var match = Regex.Match(sql, @"CREATE\s+OR\s+REPLACE\s+PROCEDURE\s+(public\.)?([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[2].Value : null; // Use Groups[2] to get the procedure name without "public."
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


                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();

                    // Get the username from the JWT token

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


    [HttpPost("CreateTableFromSql")]
    public async Task<IActionResult> CreateTableFromSql([FromBody] string sql)
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

                    var tableName = ExtractTableNameFromSql(sql);
                    if (string.IsNullOrEmpty(tableName))
                    {
                        return BadRequest("Unable to extract table name from the SQL code.");
                    }

                    // Execute the table creation SQL
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();
                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }

                    // Set the table owner to the current user
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER TABLE {tableName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();



                    // Store the created table with the user who created it
                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

                    var tableUser = new TableUser
                    {
                        Tablename = tableName,
                        UserId = user.Id
                    };

                    _context.TableUsers.Add(tableUser);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Table created successfully and ownership set to {username} for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error creating table.",
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
                message = "Table creation failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing table creation SQL.",
                error = ex.Message
            });
        }
    }

    private string ExtractTableNameFromSql(string sql)
    {
        var match = Regex.Match(sql, @"CREATE\s+TABLE\s+(public\.)?([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[2].Value : null; // Use Groups[2] to get the table name without "public."
    }
    [HttpPut("UpdateTableFromSql")]
    public async Task<IActionResult> UpdateTableFromSql([FromBody] string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return BadRequest("SQL code is required.");
        }

        // Получаем роли из JWT (предполагается, что этот метод реализован)
        var roles = GetRolesFromJwtToken();
        if (roles == null || !roles.Any())
        {
            return StatusCode(403, new { message = "No roles available to set." });
        }

        // Открываем соединение с базой данных
        await using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        // Перебираем роли – например, можно остановиться при первой успешной операции
        foreach (var role in roles)
        {
            // Каждое обновление делаем в рамках транзакции, чтобы обеспечить атомарность
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 1. Устанавливаем роль
                await using (var setRoleCmd = connection.CreateCommand())
                {
                    setRoleCmd.Transaction = transaction;
                    setRoleCmd.CommandText = $"SET ROLE \"{role}\";";
                    await setRoleCmd.ExecuteNonQueryAsync();
                }

                // 2. Извлекаем имя таблицы из переданного SQL (метод ExtractTableNameFromSql должен быть реализован)
                var tableName = ExtractTableNameFromSql(sql);
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    return BadRequest("Unable to extract table name from the SQL code.");
                }

                // 3. Проверяем, существует ли уже таблица
                bool tableExists = false;
                await using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.Transaction = transaction;
                    checkCmd.CommandText =
                        "SELECT EXISTS(" +
                        "  SELECT 1 FROM information_schema.tables " +
                        "  WHERE table_schema = 'public' AND table_name = @tableName" +
                        ");";
                    var param = checkCmd.CreateParameter();
                    param.ParameterName = "tableName";
                    param.Value = tableName;
                    checkCmd.Parameters.Add(param);
                    tableExists = (bool)await checkCmd.ExecuteScalarAsync();
                }

                // Если таблица существует – переименовываем её в резервную
                string backupTableName = null;
                if (tableExists)
                {
                    backupTableName = $"{tableName}_backup_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                    await using (var renameCmd = connection.CreateCommand())
                    {
                        renameCmd.Transaction = transaction;
                        renameCmd.CommandText = $"ALTER TABLE {tableName} RENAME TO {backupTableName};";
                        await renameCmd.ExecuteNonQueryAsync();
                    }
                }

                // 4. Создаём новую таблицу по переданному SQL‑коду
                await using (var createCmd = connection.CreateCommand())
                {
                    createCmd.Transaction = transaction;
                    createCmd.CommandText = sql;
                    await createCmd.ExecuteNonQueryAsync();
                }

                // 5. Если была резервная таблица, переносим данные
                if (tableExists)
                {
                    // Получаем список столбцов из резервной таблицы
                    var backupColumns = new List<string>();
                    await using (var backupColsCmd = connection.CreateCommand())
                    {
                        backupColsCmd.Transaction = transaction;
                        backupColsCmd.CommandText =
                            "SELECT column_name FROM information_schema.columns " +
                            "WHERE table_schema = 'public' AND table_name = @backupTable;";
                        var pBackup = backupColsCmd.CreateParameter();
                        pBackup.ParameterName = "backupTable";
                        pBackup.Value = backupTableName;
                        backupColsCmd.Parameters.Add(pBackup);

                        await using (var reader = await backupColsCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                backupColumns.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Получаем список столбцов из новой таблицы
                    var newTableColumns = new List<string>();
                    await using (var newColsCmd = connection.CreateCommand())
                    {
                        newColsCmd.Transaction = transaction;
                        newColsCmd.CommandText =
                            "SELECT column_name FROM information_schema.columns " +
                            "WHERE table_schema = 'public' AND table_name = @tableName;";
                        var pNew = newColsCmd.CreateParameter();
                        pNew.ParameterName = "tableName";
                        pNew.Value = tableName;
                        newColsCmd.Parameters.Add(pNew);

                        await using (var reader = await newColsCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                newTableColumns.Add(reader.GetString(0));
                            }
                        }
                    }

                    // Находим общие столбцы
                    var commonColumns = backupColumns.Intersect(newTableColumns, StringComparer.OrdinalIgnoreCase).ToList();
                    if (commonColumns.Any())
                    {
                        var columnsList = string.Join(", ", commonColumns.Select(c => $"\"{c}\""));
                        await using (var copyCmd = connection.CreateCommand())
                        {
                            copyCmd.Transaction = transaction;
                            // Переносим данные: вставляем в новую таблицу данные из резервной для общих столбцов
                            copyCmd.CommandText = $"INSERT INTO {tableName} ({columnsList}) SELECT {columnsList} FROM {backupTableName};";
                            await copyCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Удаляем резервную таблицу
                    await using (var dropBackupCmd = connection.CreateCommand())
                    {
                        dropBackupCmd.Transaction = transaction;
                        dropBackupCmd.CommandText = $"DROP TABLE {backupTableName};";
                        await dropBackupCmd.ExecuteNonQueryAsync();
                    }
                }

                // 6. Определяем владельца таблицы – текущего пользователя (из JWT)
                var username = User.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized("User is not authorized.");
                }
                await using (var alterCmd = connection.CreateCommand())
                {
                    alterCmd.Transaction = transaction;
                    alterCmd.CommandText = $"ALTER TABLE {tableName} OWNER TO \"{username}\";";
                    await alterCmd.ExecuteNonQueryAsync();
                }

                // 7. Сбрасываем роль
                await using (var resetRoleCmd = connection.CreateCommand())
                {
                    resetRoleCmd.Transaction = transaction;
                    resetRoleCmd.CommandText = "RESET ROLE;";
                    await resetRoleCmd.ExecuteNonQueryAsync();
                }

                // 8. Фиксируем транзакцию
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Table '{tableName}' updated successfully. Ownership set to '{username}' for role '{role}'."
                });
            }
            catch (PostgresException pgEx)
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    success = false,
                    message = "Error updating table.",
                    postgresError = pgEx.MessageText,
                    postgresDetails = pgEx.Detail,
                    postgresHint = pgEx.Hint,
                    postgresCode = pgEx.SqlState
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Можно добавить логирование ошибки здесь
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error executing table update SQL.",
                    error = ex.Message
                });
            }
        }

        // Если ни для одной из ролей операция не прошла успешно:
        return StatusCode(403, new
        {
            success = false,
            message = "Table update failed for all roles.",
            rolesTried = roles
        });
    }

    [HttpDelete("DeleteTable/{tableName}")]
    public async Task<IActionResult> DeleteTable(string tableName)
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

                    // Drop the table if it exists
                    using var command = connection.CreateCommand();
                    command.CommandText = $"DROP TABLE IF EXISTS {tableName};";
                    await command.ExecuteNonQueryAsync();

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

                    // Remove the corresponding TableUser row
                    var existingTableUser = await _context.TableUsers
                        .FirstOrDefaultAsync(tu => tu.Tablename == tableName && tu.UserId == user.Id);

                    if (existingTableUser != null)
                    {
                        _context.TableUsers.Remove(existingTableUser);
                        await _context.SaveChangesAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = $"Table '{tableName}' deleted successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error deleting table.",
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
                message = "Table deletion failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing table deletion SQL.",
                error = ex.Message
            });
        }
    }
 

}


