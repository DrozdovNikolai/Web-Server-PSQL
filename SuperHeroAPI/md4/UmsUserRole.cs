using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsUserRole
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public virtual UmsRole Role { get; set; } = null!;

    public virtual UmsUser User { get; set; } = null!;
}
