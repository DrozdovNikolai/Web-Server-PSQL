using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Patronymic { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; } = new List<CourseWork>();

    public virtual ICollection<Schedule> Schedules { get; } = new List<Schedule>();

    public virtual ICollection<Tsch> Tsches { get; } = new List<Tsch>();

    public virtual ICollection<Workload> Workloads { get; } = new List<Workload>();
}
