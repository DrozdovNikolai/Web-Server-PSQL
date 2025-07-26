using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Configuration;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    [Range(1, 3650)]
    public int RetentionDays { get; init; } = 90;

    [Range(1, 168)]
    public int CleanupIntervalHours { get; init; } = 24;
}
