using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = AuthorizationPolicies.AuditRead)]
public sealed class AuditController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private readonly DataContext _context;

    public AuditController(DataContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<AuditPageResponse>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 ||
            pageSize is < 1 or > MaximumPageSize ||
            page - 1 > int.MaxValue / Math.Max(pageSize, 1))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination",
                detail: $"Page must be at least 1 and page size must be between 1 and {MaximumPageSize}.");
        }

        var query = _context.AuditEvents.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var offset = checked((page - 1) * pageSize);
        var items = await query
            .OrderByDescending(entry => entry.RequestTime)
            .Skip(offset)
            .Take(pageSize)
            .Select(entry => new AuditEventResponse(
                entry.Id,
                entry.UserId,
                entry.Method,
                entry.Path,
                entry.StatusCode,
                entry.RequestTime,
                entry.Duration.TotalMilliseconds,
                entry.CorrelationId))
            .ToListAsync(cancellationToken);

        return Ok(new AuditPageResponse(page, pageSize, total, items));
    }
}
