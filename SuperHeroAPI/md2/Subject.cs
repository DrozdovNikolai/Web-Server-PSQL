using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Subject
{
    public int SubjectId { get; set; }

    public string? SubjectName { get; set; }

    public string? SubType { get; set; }

    public virtual ICollection<Schedule> Schedules { get; } = new List<Schedule>();

    public virtual ICollection<Workload> Workloads { get; } = new List<Workload>();
}
