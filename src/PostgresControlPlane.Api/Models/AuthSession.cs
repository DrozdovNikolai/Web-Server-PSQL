namespace PostgresControlPlane.Api.Models;

public sealed class AuthSession
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // This is the non-secret JWT ID (jti), never the bearer token itself.
    public string TokenId { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
