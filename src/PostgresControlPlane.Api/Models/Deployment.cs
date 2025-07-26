namespace PostgresControlPlane.Api.Models;

public sealed class Deployment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DatabaseHost { get; set; } = string.Empty;
    public int DatabasePort { get; set; } = 5432;
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseUser { get; set; } = string.Empty;
    public string DatabasePassword { get; set; } = string.Empty;
    public string ApplicationAdminUsername { get; set; } = string.Empty;
    public string ApplicationAdminPassword { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
