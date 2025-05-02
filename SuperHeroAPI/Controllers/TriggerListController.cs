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
public class TriggerListController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IRoleService _roleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TriggerListController(DataContext context, IRoleService roleService, IHttpContextAccessor httpContextAccessor)
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

 
[HttpPost("CreateTriggerFromSql")]
public async Task<IActionResult> CreateTriggerFromSql([FromBody] string sql)
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
                // Set the role for the current user
                using var roleCommand = connection.CreateCommand();
                roleCommand.CommandText = $"SET ROLE \"{role}\";";
                await roleCommand.ExecuteNonQueryAsync();

                var triggerName = ExtractTriggerNameFromSql(sql);
                if (string.IsNullOrEmpty(triggerName))
                {
                    return BadRequest("Unable to extract trigger name from the SQL code.");
                }

                // Execute the SQL to create the trigger
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();

                // Alter the trigger ownership to the current user
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = $"ALTER TRIGGER {triggerName} OWNER TO \"{username}\";";
                await alterCommand.ExecuteNonQueryAsync();

                // Reset role
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // Find user from the database
                var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Add entry to TriggerUsers (assuming similar logic as TableUsers)
                var triggerUser = new TriggerUser
                {
                    TriggerName = triggerName,
                    UserId = user.Id
                };

                _context.TriggerUsers.Add(triggerUser);
                await _context.SaveChangesAsync(); // Save changes to the database

                return Ok(new
                {
                    success = true,
                    message = $"Trigger '{triggerName}' created successfully for role {role}.",
                    role = role
                });
            }
            catch (PostgresException pgEx)
            {
                // Log the error and continue for other roles
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
                // Log the error and continue for other roles
                errors.Add(new { role, error = ex.Message });
                continue;
            }
        }

        // If all roles failed, return errors
        return StatusCode(403, new
        {
            success = false,
            message = "Trigger creation failed for all roles.",
            errors
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            message = "Error executing trigger creation SQL.",
            error = ex.Message
        });
    }
}


[HttpPut("UpdateTriggerFromSql")]
public async Task<IActionResult> UpdateTriggerFromSql([FromBody] string sql)
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

                var triggerName = ExtractTriggerNameFromSql(sql);
                if (string.IsNullOrEmpty(triggerName))
                {
                    return BadRequest("Unable to extract trigger name from the SQL code.");
                }

                // Выполняем удаление триггера, если он существует
                using (var dropCommand = connection.CreateCommand())
                {
                    dropCommand.CommandText = $"DROP TRIGGER IF EXISTS {triggerName} ON <table_name>;";
                    await dropCommand.ExecuteNonQueryAsync();
                }

                // Выполняем обновление триггера
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                }

                // Изменяем владельца триггера
                using (var alterCommand = connection.CreateCommand())
                {
                    alterCommand.CommandText = $"ALTER TRIGGER {triggerName} OWNER TO \"{username}\";";
                    await alterCommand.ExecuteNonQueryAsync();
                }

                // Сбрасываем роль
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // Ищем пользователя по имени
                var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Добавляем запись в таблицу TriggerUser
                var triggerUser = new TriggerUser
                {
                    TriggerName = triggerName,
                    UserId = user.Id
                };

                _context.TriggerUsers.Add(triggerUser);
                await _context.SaveChangesAsync(); // Сохраняем изменения в базе

                return Ok(new
                {
                    success = true,
                    message = $"Trigger '{triggerName}' updated successfully for role {role}.",
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

    // Если для всех ролей не удалось обновить триггер
    return StatusCode(403, new
    {
        success = false,
        message = "Trigger update failed for all roles.",
        errors
    });
}

[HttpDelete("DeleteTrigger/{triggerName}")]
public async Task<IActionResult> DeleteTrigger(string triggerName)
{
    var roles = GetRolesFromJwtToken();
    if (roles == null || roles.Count == 0)
    {
        return StatusCode(403, new { message = "No roles available to set." });
    }

    // Открываем соединение один раз для всех ролей
    using var connection = _context.Database.GetDbConnection();
    await connection.OpenAsync();

    var errors = new List<object>();

    foreach (var role in roles)
    {
        try
        {
            // Устанавливаем роль для текущего пользователя
            using var roleCommand = connection.CreateCommand();
            roleCommand.CommandText = $"SET ROLE \"{role}\";";
            await roleCommand.ExecuteNonQueryAsync();

            // Обрабатываем полное имя триггера (с учётом схемы)
            var triggerFullName = ExtractTriggerNameFromSql(triggerName);
            if (string.IsNullOrEmpty(triggerFullName))
            {
                return BadRequest("Unable to extract trigger name from the SQL code.");
            }

            // Выполняем удаление триггера
            using var command = connection.CreateCommand();
            command.CommandText = $"DROP TRIGGER IF EXISTS {triggerFullName} ON <table_name>;";
            await command.ExecuteNonQueryAsync();

            // Сбрасываем роль
            roleCommand.CommandText = "RESET ROLE";
            await roleCommand.ExecuteNonQueryAsync();

            // Возвращаем успешный ответ для этой роли
            return Ok(new
            {
                success = true,
                message = $"Trigger '{triggerFullName}' deleted successfully for role {role}.",
                role = role
            });
        }
        catch (PostgresException pgEx)
        {
            // Логируем ошибку для текущей роли и продолжаем для других
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
            // Логируем ошибку для текущей роли и продолжаем для других
            errors.Add(new { role, error = ex.Message });
            continue;
        }
    }

    // Если для всех ролей не удалось удалить триггер
    return StatusCode(403, new
    {
        success = false,
        message = "Trigger deletion failed for all roles.",
        errors
    });
}
private string ExtractTriggerNameFromSql(string sql)
{
    // Modify regex to support any schema (not just public)
    var match = Regex.Match(sql, @"CREATE\s+OR\s+REPLACE\s+TRIGGER\s+([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)|([a-zA-Z0-9_]+)\s*\(", RegexOptions.IgnoreCase);

    // If schema is present (Group 1), return "schema.trigger"
    if (match.Groups[1].Success)
    {
        return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
    }

    // If no schema is provided, return only the trigger name (Group 3)
    return match.Groups[3].Success ? match.Groups[3].Value : null;
}

}


