using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = AuthorizationPolicies.IdentityManage)]
public sealed class UserController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private readonly DataContext _context;
    private readonly TimeProvider _timeProvider;

    public UserController(DataContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<ActionResult<UserPageResponse>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOffset(page, pageSize, out var offset))
        {
            return InvalidPagination();
        }

        var query = _context.Users.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Skip(offset)
            .Take(pageSize)
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.Username,
                user.UserRoles.Select(userRole => userRole.Role.Name).ToArray()))
            .ToListAsync(cancellationToken);

        return Ok(new UserPageResponse(page, pageSize, total, users));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserSummaryResponse>> GetUser(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .Select(candidate => new UserSummaryResponse(
                candidate.Id,
                candidate.Username,
                candidate.UserRoles.Select(userRole => userRole.Role.Name).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserSummaryResponse>> UpdateUser(
        int id,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(candidate => candidate.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var revokeSessions = false;
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.Trim();
            var usernameIsTaken = await _context.Users.AnyAsync(
                candidate => candidate.Id != id && candidate.Username == normalizedUsername,
                cancellationToken);
            if (usernameIsTaken)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Username is already in use"
                });
            }

            user.Username = normalizedUsername;
            revokeSessions = true;
        }

        if (request.RoleIds is not null)
        {
            var distinctRoleIds = request.RoleIds.Distinct().ToArray();
            var roles = await _context.Roles
                .Where(role => distinctRoleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != distinctRoleIds.Length)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more roles do not exist"
                });
            }

            var removesAdministrator = user.UserRoles.Any(
                userRole => userRole.Role.Name == ApplicationRoles.SuperAdministrator) &&
                roles.All(role => role.Name != ApplicationRoles.SuperAdministrator);
            if (removesAdministrator &&
                (GetCurrentUserId() == user.Id || await CountAdministrators(cancellationToken) <= 1))
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The current or last administrator cannot lose administrative access"
                });
            }

            _context.UserRoles.RemoveRange(user.UserRoles);
            user.UserRoles = roles
                .Select(role => new UserRole { UserId = user.Id, RoleId = role.Id, Role = role })
                .ToList();
            revokeSessions = true;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
            revokeSessions = true;
        }

        if (revokeSessions)
        {
            await RevokeSessions(user.Id, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserSummaryResponse(
            user.Id,
            user.Username,
            user.UserRoles.Select(userRole => userRole.Role.Name).ToArray()));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() == id)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Administrators cannot delete their own account"
            });
        }

        var user = await _context.Users
            .Include(candidate => candidate.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (user.UserRoles.Any(userRole =>
                userRole.Role.Name == ApplicationRoles.SuperAdministrator) &&
            await CountAdministrators(cancellationToken) <= 1)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The last administrator cannot be deleted"
            });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/revoke-sessions")]
    public async Task<IActionResult> RevokeUserSessions(int id, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(user => user.Id == id, cancellationToken))
        {
            return NotFound();
        }

        await RevokeSessions(id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> RevokeSessions(int userId, CancellationToken cancellationToken)
    {
        var sessions = await _context.AuthSessions
            .Where(session => session.UserId == userId && !session.IsRevoked)
            .ToListAsync(cancellationToken);
        if (sessions.Count == 0)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = now;
        }

        return true;
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private Task<int> CountAdministrators(CancellationToken cancellationToken)
    {
        return _context.Users.CountAsync(
            user => user.UserRoles.Any(
                userRole => userRole.Role.Name == ApplicationRoles.SuperAdministrator),
            cancellationToken);
    }

    private static bool TryGetOffset(int page, int pageSize, out int offset)
    {
        offset = 0;
        if (page < 1 ||
            pageSize is < 1 or > MaximumPageSize ||
            page - 1 > int.MaxValue / Math.Max(pageSize, 1))
        {
            return false;
        }

        offset = checked((page - 1) * pageSize);
        return true;
    }

    private ObjectResult InvalidPagination()
    {
        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid pagination",
            detail: $"Page must be at least 1 and page size must be between 1 and {MaximumPageSize}.");
    }
}
