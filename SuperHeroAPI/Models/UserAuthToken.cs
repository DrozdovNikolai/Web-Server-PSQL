using System;
using System.Collections.Generic;

namespace SuperHeroAPI.Models;

public partial class UserAuthToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }
}
