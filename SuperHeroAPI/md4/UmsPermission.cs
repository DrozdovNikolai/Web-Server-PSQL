using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsPermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string TableName { get; set; } = null!;

    public int Operation { get; set; }

    public virtual UmsRole Role { get; set; } = null!;
}
