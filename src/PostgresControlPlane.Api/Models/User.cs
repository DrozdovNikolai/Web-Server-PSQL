namespace PostgresControlPlane.Api.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public ICollection<AuditEvent> AuditEvents { get; set; } = [];
    public ICollection<AuthSession> AuthSessions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
