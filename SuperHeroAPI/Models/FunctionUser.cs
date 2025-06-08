using System;
using System.Collections.Generic;

namespace SuperHeroAPI.Models;

public partial class FunctionUser
{
    public int Id { get; set; }

    public string? FunctionName { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
