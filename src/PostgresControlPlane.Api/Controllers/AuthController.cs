using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private static readonly string UnknownUserPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("not-a-real-user-password", workFactor: 12);

    private readonly DataContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public AuthController(
        DataContext context,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim();
        var user = await _context.Users
            .AsNoTracking()
            .Where(candidate => candidate.Username == normalizedUsername)
            .Select(candidate => new LoginUser(
                candidate.Id,
                candidate.Username,
                candidate.PasswordHash,
                candidate.UserRoles.Select(userRole => userRole.Role.Name).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user?.PasswordHash ?? UnknownUserPasswordHash);
        if (user is null || !passwordIsValid)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials"
            });
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes);
        var tokenId = Guid.NewGuid().ToString("N");
        var accessToken = CreateAccessToken(user, tokenId, now, expiresAt);

        _context.AuthSessions.Add(new AuthSession
        {
            UserId = user.Id,
            TokenId = tokenId,
            Expiration = expiresAt,
            IsRevoked = false
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new AuthenticationResponse(
            accessToken,
            expiresAt,
            new CurrentUserResponse(user.Id, user.Username, user.Roles)));
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityManage)]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    [HttpPost("register")]
    public async Task<ActionResult<CurrentUserResponse>> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim();
        if (await _context.Users.AnyAsync(user => user.Username == normalizedUsername, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Username is already in use"
            });
        }

        var defaultRole = await _context.Roles
            .SingleOrDefaultAsync(role => role.Name == ApplicationRoles.DefaultUser, cancellationToken);
        if (defaultRole is null)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The default application role is not configured.");
        }

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            UserRoles =
            [
                new UserRole { RoleId = defaultRole.Id }
            ]
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CurrentUserResponse(user.Id, user.Username, [defaultRole.Name]));
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId.Value)
            .Select(candidate => new CurrentUserResponse(
                candidate.Id,
                candidate.Username,
                candidate.UserRoles.Select(userRole => userRole.Role.Name).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<CurrentUserResponse>> UpdateCurrentUser(
        UpdateCurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .Include(candidate => candidate.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials"
            });
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.Trim();
            var usernameIsTaken = await _context.Users.AnyAsync(
                candidate => candidate.Id != user.Id && candidate.Username == normalizedUsername,
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
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        }

        var currentTokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var otherSessions = await _context.AuthSessions
            .Where(session =>
                session.UserId == user.Id &&
                session.TokenId != currentTokenId &&
                !session.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var session in otherSessions)
        {
            session.IsRevoked = true;
            session.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new CurrentUserResponse(
            user.Id,
            user.Username,
            user.UserRoles.Select(userRole => userRole.Role.Name).ToArray()));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return Unauthorized();
        }

        var session = await _context.AuthSessions.SingleOrDefaultAsync(
            candidate => candidate.TokenId == tokenId && !candidate.IsRevoked,
            cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        session.IsRevoked = true;
        session.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.IdentityManage)]
    [HttpGet("sessions")]
    public async Task<ActionResult<SessionPageResponse>> GetActiveSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOffset(page, pageSize, out var offset))
        {
            return InvalidPagination();
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var query = _context.AuthSessions
            .AsNoTracking()
            .Where(session => !session.IsRevoked && session.Expiration > now);
        var total = await query.CountAsync(cancellationToken);
        var sessions = await query
            .OrderByDescending(session => session.Expiration)
            .Skip(offset)
            .Take(pageSize)
            .Select(session => new SessionResponse(
                session.Id,
                session.UserId,
                session.User.Username,
                session.Expiration))
            .ToListAsync(cancellationToken);

        return Ok(new SessionPageResponse(page, pageSize, total, sessions));
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private string CreateAccessToken(LoginUser user, string tokenId, DateTime now, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture))
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private sealed record LoginUser(
        int Id,
        string Username,
        string PasswordHash,
        IReadOnlyList<string> Roles);
}
