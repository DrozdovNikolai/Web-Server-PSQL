using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsProcedureUser
{
    public int Id { get; set; }

    public string? ProcedureName { get; set; }

    public int? UserId { get; set; }

    public virtual UmsUser? User { get; set; }
}
