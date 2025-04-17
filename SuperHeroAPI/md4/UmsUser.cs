using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public virtual ICollection<UmsFunctionUser> UmsFunctionUsers { get; set; } = new List<UmsFunctionUser>();

    public virtual ICollection<UmsProcedureUser> UmsProcedureUsers { get; set; } = new List<UmsProcedureUser>();

    public virtual ICollection<UmsRequestLog> UmsRequestLogs { get; set; } = new List<UmsRequestLog>();

    public virtual ICollection<UmsTableUser> UmsTableUsers { get; set; } = new List<UmsTableUser>();

    public virtual ICollection<UmsTriggerUser> UmsTriggerUsers { get; set; } = new List<UmsTriggerUser>();

    public virtual UmsUserAuthToken? UmsUserAuthToken { get; set; }

    public virtual ICollection<UmsUserRole> UmsUserRoles { get; set; } = new List<UmsUserRole>();
}
