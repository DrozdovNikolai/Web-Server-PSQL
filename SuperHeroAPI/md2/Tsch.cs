using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Tsch
{
    public int TId { get; set; }

    public int TeacherId { get; set; }

    public int? DayId { get; set; }

    public string? Time { get; set; }

    public string? DisName { get; set; }

    public virtual Day? Day { get; set; }

    public virtual Teacher Teacher { get; set; } = null!;
}
