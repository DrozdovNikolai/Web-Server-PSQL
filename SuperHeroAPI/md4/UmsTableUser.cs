using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsTableUser
{
    public int Id { get; set; }

    public string? Tablename { get; set; }

    public int? UserId { get; set; }

    public virtual UmsUser? User { get; set; }
}
