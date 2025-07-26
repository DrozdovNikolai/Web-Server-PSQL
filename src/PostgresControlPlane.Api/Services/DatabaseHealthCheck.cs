using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PostgresControlPlane.Api.Services;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly DataContext _context;

    public DatabaseHealthCheck(DataContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The database readiness check failed.", exception);
        }
    }
}
