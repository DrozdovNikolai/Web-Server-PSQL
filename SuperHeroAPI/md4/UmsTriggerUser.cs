using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsTriggerUser
{
    public int Id { get; set; }

    public string? TriggerName { get; set; }

    public int? UserId { get; set; }

    public virtual UmsUser? User { get; set; }
}
