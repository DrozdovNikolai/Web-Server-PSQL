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

            foreach (var role in roles)
            {
                try
                {
                    // Set the role for the current user
                    using var roleCommand = connection.CreateCommand();
                    roleCommand.CommandText = $"SET ROLE \"{role}\";";
                    await roleCommand.ExecuteNonQueryAsync();

                    // Extract trigger name (you can modify the regex to suit your SQL trigger naming conventions)
                    var triggerName = ExtractTriggerNameFromSql(sql);
                    if (string.IsNullOrEmpty(triggerName))
                    {
                        return BadRequest("Unable to extract trigger name from the SQL code.");
                    }

                    // Execute the SQL to create the trigger
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

                    // Add entry to TriggerUsers (assuming similar logic as TableUsers)
                    var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username);
                    if (user == null)
                    {
                        return NotFound("User not found.");
                    }

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
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error creating trigger.",
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
                message = "Trigger creation failed for all roles.",
                rolesTried = roles
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

                    var triggerName = ExtractTriggerNameFromSql(sql);
                    if (string.IsNullOrEmpty(triggerName))
                    {
                        return BadRequest("Unable to extract trigger name from the SQL code.");
                    }

                    // Drop the trigger if it exists
                    using var dropCommand = connection.CreateCommand();
                    dropCommand.CommandText = $"DROP TRIGGER IF EXISTS {triggerName} ON <table_name>;";
                    await dropCommand.ExecuteNonQueryAsync();

                    // Execute the new trigger creation SQL (for update)
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();

                    string username = User.Identity.Name;
                    if (string.IsNullOrEmpty(username))
                    {
                        return Unauthorized("User is not authorized.");
                    }

                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Trigger '{triggerName}' updated successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error updating trigger.",
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
                message = "Trigger update failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing trigger update SQL.",
                error = ex.Message
            });
        }
    }
    [HttpDelete("DeleteTrigger/{triggerName}")]
    public async Task<IActionResult> DeleteTrigger(string triggerName)
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

                    // Drop the trigger if it exists
                    using var command = connection.CreateCommand();
                    command.CommandText = $"DROP TRIGGER IF EXISTS {triggerName} ON <table_name>;";
                    await command.ExecuteNonQueryAsync();

                    roleCommand.CommandText = "RESET ROLE";
                    await roleCommand.ExecuteNonQueryAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Trigger '{triggerName}' deleted successfully for role {role}.",
                        role = role
                    });
                }
                catch (PostgresException pgEx)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error deleting trigger.",
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
                message = "Trigger deletion failed for all roles.",
                rolesTried = roles
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Error executing trigger deletion SQL.",
                error = ex.Message
            });
        }
    }
    private string ExtractTriggerNameFromSql(string sql)
    {
        var match = Regex.Match(sql, @"CREATE\s+TRIGGER\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null; // Extracts trigger name.
    }

}


