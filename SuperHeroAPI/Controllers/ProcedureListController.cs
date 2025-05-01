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
    if (string.IsNullOrWhiteSpace(sql))
        return BadRequest("SQL code is required.");

    var roles = GetRolesFromJwtToken();
    if (roles == null || roles.Count == 0)
        return StatusCode(403, new { message = "No roles available to set." });

    // Сюда будем собирать ошибки по каждой роли
    var errors = new List<object>();

    using var connection = _context.Database.GetDbConnection();
    try
    {
        await connection.OpenAsync();

        // Попытка по каждой роли
        foreach (var role in roles)
        {
            try
            {
                // 1) Устанавливаем роль
                using var roleCmd = connection.CreateCommand();
                roleCmd.CommandText = $"SET ROLE {QuoteIdent(role)};";
                await roleCmd.ExecuteNonQueryAsync();

                // 2) Выполняем переданный SQL
                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = sql;
                await createCmd.ExecuteNonQueryAsync();

                // 3) Сбрасываем роль
                roleCmd.CommandText = "RESET ROLE;";
                await roleCmd.ExecuteNonQueryAsync();

                // 4) Извлекаем схему и имя таблицы
                var (schema, table) = ExtractSchemaAndTable(sql);
                if (string.IsNullOrEmpty(table))
                    return BadRequest("Unable to extract table name from the SQL code.");

                // 5) Альтерируем владельца на текущего пользователя
                var username = User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                    return Unauthorized("User is not authorized.");

                var fullTable = schema != null
                    ? $"{QuoteIdent(schema)}.{QuoteIdent(table)}"
                    : QuoteIdent(table);

                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {fullTable} OWNER TO {QuoteIdent(username)};";
                await alterCmd.ExecuteNonQueryAsync();

                // 6) Логируем в БД, кто создал
                var user = await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("User not found.");

                _context.TableUsers.Add(new TableUser
                {
                    Tablename = schema != null
                    ? $"{(schema)}.{(table)}"
                    : $"public.{(table)}",
                    UserId = user.Id
                });
                await _context.SaveChangesAsync();

                // Успех — сразу выходим
                return Ok(new
                {
                    success = true,
                    message = $"Table {fullTable} created and owner set to {username} under role {role}.",
                    role = role
                });
            }
            catch (PostgresException pgEx)
            {
                // Запоминаем ошибку по этой роли и пробуем следующую
                errors.Add(new
                {
                    role,
                    error = pgEx.MessageText,
                    detail = pgEx.Detail,
                    hint = pgEx.Hint,
                    code = pgEx.SqlState
                });
            }
            catch (Exception ex)
            {
                errors.Add(new { role, error = ex.Message });
            }
        }

        // Если ни одна роль не прошла — отдаем всё сразу
        return StatusCode(403, new
        {
            success = false,
            message = "Table creation failed for all roles.",
            errors
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


/// <summary>
/// Экранирует идентификатор для PostgreSQL (двойные кавычки внутри удваиваются).
/// </summary>
private string QuoteIdent(string ident)
    => "\"" + ident.Replace("\"", "\"\"") + "\"";
    [HttpPost("UpdateTableFromSql")]
    public async Task<IActionResult> UpdateTableFromSql([FromBody] string targetSql)
    {
        if (string.IsNullOrWhiteSpace(targetSql))
            return BadRequest("SQL code is required.");

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
            return StatusCode(403, new { message = "No roles available to set." });

        var (schema, table) = ExtractSchemaAndTable(targetSql);
        if (string.IsNullOrEmpty(table))
            return BadRequest("Unable to extract table name from the SQL code.");

        var fullIdent = schema != null
            ? $"{QuoteIdent(schema)}.{QuoteIdent(table)}"
            : QuoteIdent(table);

        // --- Получаем текущее DDL ---
        string currentSql;
        await _context.Database.OpenConnectionAsync();
        try
        {
            var conn = _context.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT ums.select_table_definition('{schema}.{table}')";
            currentSql = (await cmd.ExecuteScalarAsync())?.ToString()
                         ?? throw new Exception("Cannot fetch current table DDL.");
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        var currentDef = DefinitionParser.Parse(currentSql);
        var targetDef = DefinitionParser.Parse(targetSql);
        var alters = SchemaDiffer.GenerateChangeScripts(schema, table, currentDef, targetDef);

        if (!alters.Any())
            return Ok(new { success = true, message = "Schema is up to date, no changes needed." });

        var errors = new List<object>();

        // --- Перебираем роли ---
        foreach (var role in roles)
        {
            await _context.Database.OpenConnectionAsync();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conn = _context.Database.GetDbConnection();

                // SET ROLE
                using (var roleCmd = conn.CreateCommand())
                {
                    roleCmd.CommandText = $"SET ROLE {QuoteIdent(role)};";
                    await roleCmd.ExecuteNonQueryAsync();
                }

                // APPLY ALTER SCRIPTS
                var executed = new List<string>();
                foreach (var sql in alters)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync();
                    executed.Add(sql);
                }

                // RESET ROLE
                using (var roleCmd = conn.CreateCommand())
                {
                    roleCmd.CommandText = "RESET ROLE;";
                    await roleCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Schema updated successfully under role '{role}'.",
                    role,
                    scripts = executed
                });
            }
            catch (PostgresException pgEx)
            {
                await tx.RollbackAsync();
                errors.Add(new
                {
                    role,
                    error = pgEx.MessageText,
                    detail = pgEx.Detail,
                    hint = pgEx.Hint,
                    code = pgEx.SqlState
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                errors.Add(new { role, error = ex.Message });
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        return StatusCode(403, new
        {
            success = false,
            message = "Schema update failed for all roles.",
            errors
        });
    }
    [HttpPost("DeleteTable")]
    public async Task<IActionResult> DeleteTable([FromBody] string tableNameInput)
    {
        if (string.IsNullOrWhiteSpace(tableNameInput))
            return BadRequest("Table name is required.");

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
            return StatusCode(403, new { message = "No roles available to set." });

        // Парсим схему и имя
        var (schema, table) = ExtractSchemaAndTable($"CREATE TABLE {tableNameInput} (x int);");
        if (string.IsNullOrEmpty(table))
            return BadRequest("Unable to extract table name (and schema) from input.");

        var fullIdent = schema != null
            ? $"{QuoteIdent(schema)}.{QuoteIdent(table)}"
            : QuoteIdent(table);

        var errors = new List<object>();

        foreach (var role in roles)
        {
            // Открываем соединение под контролем EF
            await _context.Database.OpenConnectionAsync();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conn = _context.Database.GetDbConnection();

                // 1) Устанавливаем роль
                using (var roleCmd = conn.CreateCommand())
                {
                    roleCmd.CommandText = $"SET ROLE {QuoteIdent(role)};";
                    await roleCmd.ExecuteNonQueryAsync();
                }

                // 2) Удаляем таблицу, если она есть
                using (var dropCmd = conn.CreateCommand())
                {
                    dropCmd.CommandText = $"DROP TABLE IF EXISTS {fullIdent} CASCADE;";
                    await dropCmd.ExecuteNonQueryAsync();
                }

                // 3) Сбрасываем роль
                using (var roleCmd = conn.CreateCommand())
                {
                    roleCmd.CommandText = "RESET ROLE;";
                    await roleCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Table {fullIdent} was dropped under role '{role}'.",
                    role
                });
            }
            catch (PostgresException pgEx)
            {
                await tx.RollbackAsync();
                errors.Add(new
                {
                    role,
                    error = pgEx.MessageText,
                    detail = pgEx.Detail,
                    hint = pgEx.Hint,
                    code = pgEx.SqlState
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                errors.Add(new { role, error = ex.Message });
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        return StatusCode(403, new
        {
            success = false,
            message = "Table drop failed for all roles.",
            errors
        });
    }

    #region Helpers

    private (string Schema, string Table) ExtractSchemaAndTable(string sql)
    {
        var pattern =
            @"CREATE\s+TABLE\s+" +
            @"(?:(?:" +
              @"""(?<schema>[^""]+)""|\b(?<schema>\w+)\b" +
            @")\.)?" +
            @"(?:" +
              @"""(?<table>[^""]+)""|\b(?<table>\w+)\b" +
            @")";
        var m = Regex.Match(sql, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return (null, null);
        var schema = m.Groups["schema"].Success ? m.Groups["schema"].Value : null;
        var table = m.Groups["table"].Value;
        return (schema, table);
    }


    #endregion
}

// ===== Модели и парсер =====

public class TableDefinition
{
    public List<ColumnDef> Columns { get; set; } = new();
    public List<ConstraintDef> Constraints { get; set; } = new();
    public List<IndexDef> Indexes { get; set; } = new();
}

public class ColumnDef
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public string Default { get; set; }
    public bool NotNull { get; set; }
}

public class ConstraintDef
{
    public string Name { get; set; }
    public string Definition { get; set; }  // без префикса "CONSTRAINT name"
}

public class IndexDef
{
    public string Definition { get; set; }  // полная строка "CREATE [UNIQUE] INDEX …"
}

public static class DefinitionParser
{
    /// <summary>
    /// Простенький парсер на основе регулярных выражений.
    /// Можно доработать, чтобы поддерживать всё, что нужно.
    /// </summary>
    public static TableDefinition Parse(string ddl)
    {
        var def = new TableDefinition();

        // 1. Найти начало CREATE TABLE и первую '('
        var createIdx = Regex.Match(ddl, @"CREATE\s+TABLE", RegexOptions.IgnoreCase);
        if (!createIdx.Success) return def;

        var parenStart = ddl.IndexOf('(', createIdx.Index);
        if (parenStart < 0) return def;

        // 2. Собрать текст до matching ')'
        int depth = 0;
        int i = parenStart;
        int blockStart = parenStart + 1;
        int blockEnd = -1;

        for (; i < ddl.Length; i++)
        {
            if (ddl[i] == '(') depth++;
            else if (ddl[i] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    blockEnd = i;
                    break;
                }
            }
        }
        if (blockEnd < 0) return def;

        var inner = ddl.Substring(blockStart, blockEnd - blockStart);

        // 3. Разбить inner на топ-левел строки
        var lines = new List<string>();
        var sb = new StringBuilder();
        depth = 0;
        foreach (var ch in inner)
        {
            if (ch == '(')
            {
                depth++;
                sb.Append(ch);
            }
            else if (ch == ')')
            {
                depth--;
                sb.Append(ch);
            }
            else if (ch == ',' && depth == 0)
            {
                // конец строки
                var line = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        // последний кусок
        var last = sb.ToString().Trim();
        if (!string.IsNullOrEmpty(last))
            lines.Add(last);

        // 4. Парсинг строк
        foreach (var line in lines)
        {
            // 4.1 COLUMN: starts with identifier
            var colMatch = Regex.Match(line,
                @"^(?<name>""?[\w]+""?)\s+(?<type>.+?)(?:\s+DEFAULT\s+(?<def>.+))?(?:\s+NOT\s+NULL)?$",
                RegexOptions.IgnoreCase);
            if (colMatch.Success)
            {
                def.Columns.Add(new ColumnDef
                {
                    Name = colMatch.Groups["name"].Value,
                    DataType = colMatch.Groups["type"].Value.Trim(),
                    Default = colMatch.Groups["def"].Success
                                 ? colMatch.Groups["def"].Value.Trim()
                                 : null,
                    NotNull = line.ToUpperInvariant().Contains("NOT NULL")
                });
                continue;
            }

            // 4.2 CONSTRAINT
            if (line.TrimStart().StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    def.Constraints.Add(new ConstraintDef
                    {
                        Name = parts[1],
                        Definition = parts[2].Trim()
                    });
                }
                continue;
            }
        }

        // 5. Индексы — ищем только после всего CREATE TABLE, до ';'
        foreach (Match m in Regex.Matches(ddl, @"CREATE\s+(UNIQUE\s+)?INDEX.+?;", RegexOptions.IgnoreCase))
        {
            def.Indexes.Add(new IndexDef { Definition = m.Value.TrimEnd(';').Trim() });
        }

        return def;
    }
}

// ===== Дифф и генерация ALTER =====

public static class SchemaDiffer
{
    public static List<string> GenerateChangeScripts(
         string schema,
         string table,
         TableDefinition current,
         TableDefinition target)
    {
        var fullName = schema != null
            ? $"\"{schema}\".\"{table}\""
            : $"\"{table}\"";

        var scripts = new List<string>();

        //
        // 1) Колонки (ADD, ALTER TYPE/DEFAULT/NULL, DROP)
        //
        var curCols = current.Columns
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        var tgtCols = target.Columns
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        // 1.1 Add or alter existing
        foreach (var kv in tgtCols)
        {
            var name = kv.Key;
            var col = kv.Value;
            if (!curCols.ContainsKey(name))
            {
                // ADD COLUMN
                var sql = $"ALTER TABLE {fullName} ADD COLUMN {col.Name} {col.DataType}" +
                          (col.Default != null ? $" DEFAULT {col.Default}" : "") +
                          (col.NotNull ? " NOT NULL" : "");
                scripts.Add(sql + ";");
            }
            else
            {
                var old = curCols[name];
                if (!string.Equals(old.DataType, col.DataType, StringComparison.OrdinalIgnoreCase))
                    scripts.Add($"ALTER TABLE {fullName} ALTER COLUMN {name} TYPE {col.DataType};");

                if (old.Default != col.Default)
                {
                    if (col.Default != null)
                        scripts.Add($"ALTER TABLE {fullName} ALTER COLUMN {name} SET DEFAULT {col.Default};");
                    else
                        scripts.Add($"ALTER TABLE {fullName} ALTER COLUMN {name} DROP DEFAULT;");
                }

                if (old.NotNull != col.NotNull)
                    scripts.Add($"ALTER TABLE {fullName} ALTER COLUMN {name} {(col.NotNull ? "SET" : "DROP")} NOT NULL;");
            }
        }
        // 1.2 Drop columns missing in target
        foreach (var name in curCols.Keys.Except(tgtCols.Keys, StringComparer.OrdinalIgnoreCase))
        {
            scripts.Add($"ALTER TABLE {fullName} DROP COLUMN {name} CASCADE;");
        }

        //
        // 2) Constraints
        //
        // 2.1 Сгруппировать и убрать дубли в current и target
        var curCons = current.Constraints
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        var tgtCons = target.Constraints
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        // 2.2 Добавить новые и обновить изменённые
        foreach (var kv in tgtCons)
        {
            var name = kv.Key;
            var def = kv.Value.Definition;
            if (!curCons.ContainsKey(name))
            {
                scripts.Add($"ALTER TABLE {fullName} ADD CONSTRAINT {name} {def};");
            }
            else if (!string.Equals(curCons[name].Definition, def, StringComparison.OrdinalIgnoreCase))
            {
                scripts.Add($"ALTER TABLE {fullName} DROP CONSTRAINT {name} CASCADE;");
                scripts.Add($"ALTER TABLE {fullName} ADD CONSTRAINT {name} {def};");
            }
        }
        // 2.3 Удалить лишние
        foreach (var name in curCons.Keys.Except(tgtCons.Keys, StringComparer.OrdinalIgnoreCase))
        {
            scripts.Add($"ALTER TABLE {fullName} DROP CONSTRAINT {name} CASCADE;");
        }

        //
        // 3) Индексы
        //
        var curIdx = new HashSet<string>(current.Indexes.Select(i => i.Definition), StringComparer.OrdinalIgnoreCase);
        foreach (var idx in target.Indexes)
        {
            if (!curIdx.Contains(idx.Definition))
                scripts.Add(idx.Definition + ";");
        }
        // (По желанию: удаление индексов, которых нет в целевом, аналогично constraints)

        return scripts;
    }
}



