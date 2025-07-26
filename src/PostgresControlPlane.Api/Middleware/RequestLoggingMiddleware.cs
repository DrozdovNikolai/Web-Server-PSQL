using System.Diagnostics;
using System.Security.Claims;
using PostgresControlPlane.Api.Data;

namespace PostgresControlPlane.Api.Middleware;

/// <summary>
/// Stores a metadata-only audit trail. Request/response bodies, authorization
/// headers, cookies, SQL text, passwords and bearer tokens are never persisted.
/// </summary>
public sealed partial class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, DataContext dbContext)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var startedAt = DateTime.UtcNow;
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            var auditLog = new AuditEvent
            {
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                CorrelationId = context.TraceIdentifier,
                RequestTime = startedAt,
                ResponseTime = DateTime.UtcNow,
                Duration = Stopwatch.GetElapsedTime(timestamp),
                StatusCode = context.Response.StatusCode,
                UserId = GetUserId(context.User)
            };

            try
            {
                dbContext.AuditEvents.Add(auditLog);
                await dbContext.SaveChangesAsync(context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                LogAuditPersistenceCancelled(_logger, context.Request.Method, context.Request.Path);
            }
            catch (Exception exception)
            {
                LogAuditPersistenceFailed(
                    _logger,
                    context.Request.Method,
                    context.Request.Path,
                    exception);
            }
        }
    }

    private static int? GetUserId(ClaimsPrincipal principal)
    {
        return int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    private static bool ShouldSkip(PathString path)
    {
        return path.StartsWithSegments("/health") ||
               path.StartsWithSegments("/swagger");
    }

    [LoggerMessage(1, LogLevel.Debug, "Audit log persistence was cancelled for {Method} {Path}")]
    private static partial void LogAuditPersistenceCancelled(
        ILogger logger,
        string method,
        PathString path);

    [LoggerMessage(2, LogLevel.Warning, "Failed to persist audit metadata for {Method} {Path}")]
    private static partial void LogAuditPersistenceFailed(
        ILogger logger,
        string method,
        PathString path,
        Exception exception);
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
