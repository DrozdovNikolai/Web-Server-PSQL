using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsRole
{
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<UmsGlobalpermission> UmsGlobalpermissions { get; set; } = new List<UmsGlobalpermission>();

    public virtual ICollection<UmsPermission> UmsPermissions { get; set; } = new List<UmsPermission>();

    public virtual ICollection<UmsUserRole> UmsUserRoles { get; set; } = new List<UmsUserRole>();
}
