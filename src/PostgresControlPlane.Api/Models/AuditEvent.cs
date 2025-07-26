namespace PostgresControlPlane.Api.Models;

public sealed class AuditEvent
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime RequestTime { get; set; }
    public DateTime ResponseTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public User? User { get; set; }
}
