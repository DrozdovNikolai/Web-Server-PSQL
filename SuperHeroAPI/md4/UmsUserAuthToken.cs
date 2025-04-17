using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsUserAuthToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual UmsUser IdNavigation { get; set; } = null!;
}
