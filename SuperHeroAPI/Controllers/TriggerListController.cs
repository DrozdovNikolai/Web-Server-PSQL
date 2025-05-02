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
            return BadRequest("SQL code is required.");

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
            return StatusCode(403, new { message = "No roles available to set." });

        // Открываем соединение один раз
        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        string username = User.Identity.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("User is not authorized.");

        var errors = new List<object>();

        foreach (var role in roles)
        {
            try
            {
                // 1) Set role
                using var roleCommand = connection.CreateCommand();
                roleCommand.CommandText = $"SET ROLE \"{role}\";";
                await roleCommand.ExecuteNonQueryAsync();

                // 2) Создаём триггер
                var triggerName = ExtractTriggerNameFromSql(sql);
                if (string.IsNullOrEmpty(triggerName))
                    return BadRequest("Unable to extract trigger name from the SQL code.");

                using (var createCmd = connection.CreateCommand())
                {
                    createCmd.CommandText = sql;
                    await createCmd.ExecuteNonQueryAsync();
                }

                // 3) Reset role
                roleCommand.CommandText = "RESET ROLE";
                await roleCommand.ExecuteNonQueryAsync();

                // 4) Запись в TriggerUsers
                var user = await _context.Users.AsNoTracking()
                                               .SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("User not found.");

                var triggerUser = new TriggerUser
                {
                    TriggerName = triggerName,
                    UserId = user.Id
                };
                _context.TriggerUsers.Add(triggerUser);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Trigger '{triggerName}' created successfully for role {role}.",
                    role
                });
            }
            catch (PostgresException pgEx)
            {
                // Логируем ошибку и пробуем следующую роль
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
                errors.Add(new { role, error = ex.Message });
                continue;
            }
        }

        return StatusCode(403, new
        {
            success = false,
            message = "Trigger creation failed for all roles.",
            errors
        });
    }


    [HttpPut("UpdateTriggerFromSql")]
    public async Task<IActionResult> UpdateTriggerFromSql([FromBody] string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return BadRequest("SQL code is required.");

        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
            return StatusCode(403, new { message = "No roles available to set." });

        // Открываем соединение один раз
        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        string username = User.Identity.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("User is not authorized.");

        var errors = new List<object>();

        foreach (var role in roles)
        {
            try
            {
                // Устанавливаем роль
                using var roleCmd = connection.CreateCommand();
                roleCmd.CommandText = $"SET ROLE \"{role}\";";
                await roleCmd.ExecuteNonQueryAsync();

                // Извлекаем имя триггера и таблицы
                var (triggerName, tableName) = ExtractTriggerInfoFromSql(sql);
                if (string.IsNullOrEmpty(triggerName) || string.IsNullOrEmpty(tableName))
                    return BadRequest("Unable to extract trigger or table name from the SQL code.");

                // 1) DROP TRIGGER IF EXISTS schema.trigger ON schema.table;
                using (var dropCmd = connection.CreateCommand())
                {
                    dropCmd.CommandText = $"DROP TRIGGER IF EXISTS {triggerName} ON {tableName};";
                    await dropCmd.ExecuteNonQueryAsync();
                }

                // 2) CREATE TRIGGER … (сама SQL-строка из body)
                using (var createCmd = connection.CreateCommand())
                {
                    createCmd.CommandText = sql;
                    await createCmd.ExecuteNonQueryAsync();
                }

                // 3) Сброс роли
                roleCmd.CommandText = "RESET ROLE;";
                await roleCmd.ExecuteNonQueryAsync();

                // 4) Логируем в TriggerUsers
                var user = await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("User not found.");

                // Удалим старую запись, если есть
                var existing = await _context.TriggerUsers
                    .FirstOrDefaultAsync(tu => tu.TriggerName == triggerName && tu.UserId == user.Id);
                if (existing != null)
                    _context.TriggerUsers.Remove(existing);

                // Добавим новую
                _context.TriggerUsers.Add(new TriggerUser
                {
                    TriggerName = triggerName,
                    UserId = user.Id
                });
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Trigger '{triggerName}' updated successfully for role {role}.",
                    role
                });
            }
            catch (PostgresException pgEx)
            {
                // Логируем ошибку и пробуем следующую роль
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
                errors.Add(new { role, error = ex.Message });
                continue;
            }
        }

        // Если ни одна роль не сработала
        return StatusCode(403, new
        {
            success = false,
            message = "Trigger update failed for all roles.",
            errors
        });
    }

    private (string TriggerName, string TableName) ExtractTriggerInfoFromSql(string sql)
    {
        var match = Regex.Match(sql,
            @"CREATE\s+(?:OR\s+REPLACE\s+)?TRIGGER\s+" +
            @"(?:(?<tschema>[\w]+)\.)?(?<trigger>[\w]+)\s+" +
            @"(?:BEFORE|AFTER|INSTEAD\s+OF).*?\s+ON\s+" +
            @"(?:(?<schema>[\w]+)\.)?(?<table>[\w]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
            return (null, null);

        var trig = match.Groups["tschema"].Success
            ? $"{match.Groups["tschema"].Value}.{match.Groups["trigger"].Value}"
            : match.Groups["trigger"].Value;

        var tbl = match.Groups["schema"].Success
            ? $"{match.Groups["schema"].Value}.{match.Groups["table"].Value}"
            : match.Groups["table"].Value;

        return (trig, tbl);
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
    [HttpDelete("DeleteTrigger/{tableName}/{triggerName}")]
    public async Task<IActionResult> DeleteTrigger(string tableName, string triggerName)
    {
        var roles = GetRolesFromJwtToken();
        if (roles == null || roles.Count == 0)
            return StatusCode(403, new { message = "No roles available to set." });

        // Открываем соединение один раз
        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        string username = User.Identity.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized("User is not authorized.");

        // Разбираем schema и имя
        (var tblSchema, var tbl) = SplitQualifiedName(tableName);
        (var trgSchema, var trg) = SplitQualifiedName(triggerName);

        var tableFull = tblSchema != null
            ? $"{QuoteIdent(tblSchema)}.{QuoteIdent(tbl)}"
            : QuoteIdent(tbl);
        var triggerFull = trgSchema != null
            ? $"{QuoteIdent(trgSchema)}.{QuoteIdent(trg)}"
            : QuoteIdent(trg);

        var errors = new List<object>();

        foreach (var role in roles)
        {
            try
            {
                // 1) SET ROLE
                using var roleCmd = connection.CreateCommand();
                roleCmd.CommandText = $"SET ROLE {QuoteIdent(role)};";
                await roleCmd.ExecuteNonQueryAsync();

                // 2) DROP TRIGGER
                using var dropCmd = connection.CreateCommand();
                dropCmd.CommandText = $"DROP TRIGGER IF EXISTS {triggerFull} ON {tableFull};";
                await dropCmd.ExecuteNonQueryAsync();

                // 3) RESET ROLE
                roleCmd.CommandText = "RESET ROLE;";
                await roleCmd.ExecuteNonQueryAsync();

                // 4) Удаляем запись в TriggerUsers
                var user = await _context.Users.AsNoTracking()
                                               .SingleOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return NotFound("User not found.");

                var existing = await _context.TriggerUsers
                    .FirstOrDefaultAsync(tu => tu.TriggerName == triggerFull && tu.UserId == user.Id);
                if (existing != null)
                {
                    _context.TriggerUsers.Remove(existing);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"Trigger '{triggerFull}' deleted successfully for role {role}.",
                    role
                });
            }
            catch (PostgresException pgEx)
            {
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
                errors.Add(new { role, error = ex.Message });
                continue;
            }
        }

        return StatusCode(403, new
        {
            success = false,
            message = "Trigger deletion failed for all roles.",
            errors
        });
    }

    private (string schema, string name) SplitQualifiedName(string input)
    {
        // "schema.name" либо просто "name"
        if (input.Contains('.'))
        {
            var parts = input.Split('.', 2);
            return (parts[0], parts[1]);
        }
        return (null, input);
    }
    private string QuoteIdent(string ident)
    => "\"" + ident.Replace("\"", "\"\"") + "\"";
    private string ExtractTriggerNameFromSql(string sql)
    {
        // Ищем схему и имя сразу после "CREATE [OR REPLACE] TRIGGER"
        var match = Regex.Match(sql,
            @"CREATE\s+(?:OR\s+REPLACE\s+)?TRIGGER\s+" +        // начало создания триггера
            @"(?:(?<schema>[\w]+)\.)?(?<trigger>[\w]+)\b",      // схема и имя триггера
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return null;

        // Если схема указана, возвращаем "schema.trigger", иначе просто "trigger"
        if (match.Groups["schema"].Success)
            return $"{match.Groups["schema"].Value}.{match.Groups["trigger"].Value}";
        else
            return match.Groups["trigger"].Value;
    }

}


