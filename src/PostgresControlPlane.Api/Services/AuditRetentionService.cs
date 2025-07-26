using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;

namespace PostgresControlPlane.Api.Services;

public sealed partial class AuditRetentionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditRetentionService> _logger;

    public AuditRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<AuditOptions> options,
        TimeProvider timeProvider,
        ILogger<AuditRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(_options.CleanupIntervalHours),
            _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DeleteExpiredEvents(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogCleanupFailure(_logger, exception);
            }
        }
    }

    private async Task DeleteExpiredEvents(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now.AddDays(-_options.RetentionDays);
        var deletedCount = await context.AuditEvents
            .Where(auditEvent => auditEvent.RequestTime < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedSessions = await context.AuthSessions
            .Where(session => session.Expiration < now)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            LogExpiredEventsDeleted(_logger, deletedCount);
        }

        if (deletedSessions > 0)
        {
            LogExpiredSessionsDeleted(_logger, deletedSessions);
        }
    }

    [LoggerMessage(1, LogLevel.Information, "Deleted {DeletedCount} expired audit events")]
    private static partial void LogExpiredEventsDeleted(ILogger logger, int deletedCount);

    [LoggerMessage(2, LogLevel.Warning, "Failed to enforce the audit retention policy")]
    private static partial void LogCleanupFailure(ILogger logger, Exception exception);

    [LoggerMessage(3, LogLevel.Information, "Deleted {DeletedCount} expired authentication sessions")]
    private static partial void LogExpiredSessionsDeleted(ILogger logger, int deletedCount);
}
