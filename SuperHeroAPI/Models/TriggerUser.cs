using System;
using System.Collections.Generic;

namespace SuperHeroAPI.Models;

public partial class TriggerUser
{
    public int Id { get; set; }

    public string? TriggerName { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
