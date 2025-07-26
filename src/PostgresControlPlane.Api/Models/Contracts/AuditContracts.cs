namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record AuditEventResponse(
    int Id,
    int? ActorId,
    string Method,
    string Path,
    int StatusCode,
    DateTime StartedAt,
    double DurationMilliseconds,
    string CorrelationId);

public sealed record AuditPageResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<AuditEventResponse> Items);
