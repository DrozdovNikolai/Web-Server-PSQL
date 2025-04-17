using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models;

[Table("ums_user_auth_tokens", Schema = "ums")]
public class UserAuthToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("expiration")]
    public DateTime Expiration { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
