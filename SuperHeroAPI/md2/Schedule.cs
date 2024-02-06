using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int AudId { get; set; }

    public int? DayId { get; set; }

    public string? Timerange { get; set; }

    public int SubjectId { get; set; }

    public int TeacherId { get; set; }

    public int GroupId { get; set; }

    public virtual Auditorium Aud { get; set; } = null!;

    public virtual Day? Day { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
