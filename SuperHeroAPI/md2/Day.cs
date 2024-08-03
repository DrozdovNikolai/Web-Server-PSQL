using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Day
{
    public int DayId { get; set; }

    public string? Dayofweek { get; set; }

    public virtual ICollection<Schedule> Schedules { get; } = new List<Schedule>();

    public virtual ICollection<Teachschedule> Teachschedules { get; } = new List<Teachschedule>();

    public virtual ICollection<Tsch> Tsches { get; } = new List<Tsch>();
}
