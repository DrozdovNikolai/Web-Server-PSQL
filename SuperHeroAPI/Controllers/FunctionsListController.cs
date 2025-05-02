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
using static ProcedureListController;

[Route("api/[controller]")]
[ApiController, Authorize]
public class FunctionsListController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IRoleService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FunctionsListController(DataContext context, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _roleService = roleService;
        _httpContextAccessor = httpContextAccessor;
    }

    public class FunctionRequest
    {
        public string FunctionName { get; set; }
        public List<ProcedureParameter> Parameters { get; set; }
        public string Body { get; set; } // SQL logic for the function
        public string ReturnType { get; set; } // Return type of the function (e.g., integer, text, json, etc.)
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

    // Открываем соединение один раз для всех ролей
    using var connection = _context.Database.GetDbConnection();
    await connection.OpenAsync();

    string username = User.Identity.Name;
    if (string.IsNullOrEmpty(username))
    {
        return Unauthorized("User is not authorized.");
    }

    var errors = new List<object>();

    foreach (var role in roles)
    {
        try
        {
            // Устанавливаем роль для текущего пользователя
            using (var roleCommand = connection.CreateCommand())
            {
                roleCommand.CommandText = $"SET ROLE \"{role}\";";
                await roleCommand.ExecuteNonQueryAsync();

                var functionName = ExtractFunctionNameFromSql(sql);
                if (string.IsNullOrEmpty(functionName))
                {
                    return BadRequest("Unable to extract function name from the SQL code.");
                }

                // Выполняем SQL для удаления функции, если она существует
                using (var dropCommand = connection.CreateCommand())
                {
                    dropCommand.CommandText = $"DROP FUNCTION IF EXISTS {functionName};";
                    await dropCommand.ExecuteNonQueryAsync();
                }

                // Выполняем обновление функции (динамическое создание)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                }

                // Устанавливаем владельца функции
                using (var alterCommand = connection.CreateCommand())
                {
                    alterCommand.CommandText = $"ALTER FUNCTION {functionName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                }

                // Сбрасываем роль
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // Записываем успешный результат для этой роли
                return Ok(new
                {
                    success = true,
                    message = $"Function updated successfully and ownership set to {username} for role {role}.",
                    role = role
                });
            }
        }
        catch (PostgresException pgEx)
        {
            // Логируем и продолжаем для других ролей
            errors.Add(new
            {
                role,
                error = pgEx.MessageText,
                detail = pgEx.Detail,
                hint = pgEx.Hint,
                code = pgEx.SqlState
            });
            continue;
        }
        catch (Exception ex)
        {
            // Логируем и продолжаем для других ролей
            errors.Add(new { role, error = ex.Message });
            continue;
        }
    }

    // Если все роли не прошли — возвращаем ошибку
    return StatusCode(403, new
    {
        success = false,
        message = "Function update failed for all roles.",
        errors
    });
}

[HttpDelete("DeleteFunction/{functionName}")]
public async Task<IActionResult> DeleteFunction(string functionName)
{
    var roles = GetRolesFromJwtToken();
    if (roles == null || roles.Count == 0)
    {
        return StatusCode(403, new { message = "No roles available to set." });
    }

    // Открываем соединение один раз для всех ролей
    using var connection = _context.Database.GetDbConnection();
    await connection.OpenAsync();

    string username = User.Identity.Name;
    if (string.IsNullOrEmpty(username))
    {
        return Unauthorized("User is not authorized.");
    }

    var errors = new List<object>();

    foreach (var role in roles)
    {
        try
        {
            // Устанавливаем роль для текущего пользователя
            using (var roleCommand = connection.CreateCommand())
            {
                roleCommand.CommandText = $"SET ROLE \"{role}\";";
                await roleCommand.ExecuteNonQueryAsync();

                // Выполняем удаление функции, если она существует
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"DROP FUNCTION IF EXISTS {functionName};";
                    await command.ExecuteNonQueryAsync();
                }

                // Сбрасываем роль
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // Находим пользователя из JWT
                var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Удаляем соответствующую запись в FunctionUser
                var existingFunctionUser = await _context.FunctionUsers
                    .FirstOrDefaultAsync(fu => fu.FunctionName == functionName && fu.UserId == user.Id);

                if (existingFunctionUser != null)
                {
                    _context.FunctionUsers.Remove(existingFunctionUser);
                    await _context.SaveChangesAsync(); // Сохраняем изменения в базе
                }

                return Ok(new
                {
                    success = true,
                    message = $"Function '{functionName}' deleted successfully for role {role}.",
                    role = role
                });
            }
        }
        catch (PostgresException pgEx)
        {
            // Логируем ошибку и продолжаем для других ролей
            errors.Add(new
            {
                role,
                error = pgEx.MessageText,
                detail = pgEx.Detail,
                hint = pgEx.Hint,
                code = pgEx.SqlState
            });
            continue;
        }
        catch (Exception ex)
        {
            // Логируем ошибку и продолжаем для других ролей
            errors.Add(new { role, error = ex.Message });
            continue;
        }
    }

    // Если для всех ролей не удалось удалить функцию
    return StatusCode(403, new
    {
        success = false,
        message = "Delete failed for all roles.",
        errors
    });
}


private string ExtractFunctionNameFromSql(string sql)
{
    // Используем регулярное выражение, чтобы извлечь схему и имя функции
    var match = Regex.Match(sql, @"CREATE\s+OR\s+REPLACE\s+FUNCTION\s+([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)|([a-zA-Z0-9_]+)\s*\(", RegexOptions.IgnoreCase);

    // Если есть совпадение, то:
    if (match.Success)
    {
        // Если схема указана, то возвращаем полное имя в формате "schema.function"
        if (match.Groups[1].Success)
        {
            return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
        }
        // Если схемы нет, то возвращаем только имя функции
        return match.Groups[3].Value;
    }
    
    return null; // Если совпадение не найдено
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

    // Открываем соединение один раз для всех ролей
    using var connection = _context.Database.GetDbConnection();
    await connection.OpenAsync();

    string username = User.Identity.Name;
    if (string.IsNullOrEmpty(username))
    {
        return Unauthorized("User is not authorized.");
    }

    // Перебираем роли
    foreach (var role in roles)
    {
        try
        {
            // Устанавливаем роль для текущего пользователя
            using (var roleCommand = connection.CreateCommand())
            {
                roleCommand.CommandText = $"SET ROLE \"{role}\";";
                await roleCommand.ExecuteNonQueryAsync();

                var functionName = ExtractFunctionNameFromSql(sql);
                if (string.IsNullOrEmpty(functionName))
                {
                    return BadRequest("Unable to extract function name from the SQL code.");
                }

                // Выполняем SQL для создания функции
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                }

                // Устанавливаем владельца функции
                using (var alterCommand = connection.CreateCommand())
                {
                    alterCommand.CommandText = $"ALTER FUNCTION {functionName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                }

                // Сбрасываем роль
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // Создаём запись в таблице FunctionUser
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

                // Возвращаем успех для текущей роли
                return Ok(new
                {
                    success = true,
                    message = $"Function created successfully and ownership set to {username} for role {role}.",
                    role = role
                });
            }
        }
        catch (PostgresException pgEx)
        {
            // Логируем и продолжаем для других ролей
            Console.WriteLine($"Error creating function for role {role}: {pgEx.MessageText}");
            continue;
        }
        catch (Exception ex)
        {
            // Логируем и продолжаем для других ролей
            Console.WriteLine($"Error creating function for role {role}: {ex.Message}");
            continue;
        }
    }

    return StatusCode(403, new
    {
        success = false,
        message = "Function creation failed for all roles.",
        rolesTried = roles
    });
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


