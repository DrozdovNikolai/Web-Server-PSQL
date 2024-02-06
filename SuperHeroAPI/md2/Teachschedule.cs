using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Teachschedule
{
    public int LessonId { get; set; }

    public int WlId { get; set; }

    public string? Time { get; set; }

    public int? DayId { get; set; }

    public virtual Day? Day { get; set; }

    public virtual Workload Wl { get; set; } = null!;
}
