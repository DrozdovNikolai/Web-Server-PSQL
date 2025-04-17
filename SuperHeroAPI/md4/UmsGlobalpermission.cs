using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsGlobalpermission
{
    public int PermissionId { get; set; }

    public int RoleId { get; set; }

    public bool CreateTableGrant { get; set; }

    public bool UpdateTableGrant { get; set; }

    public bool DeleteTableGrant { get; set; }

    public bool CreateGrant { get; set; }

    public virtual UmsRole Role { get; set; } = null!;
}
