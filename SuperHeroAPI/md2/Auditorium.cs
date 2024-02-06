using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Auditorium
{
    public int AudId { get; set; }

    public string? Number { get; set; }

    public string? Type { get; set; }

    public int? Count { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
