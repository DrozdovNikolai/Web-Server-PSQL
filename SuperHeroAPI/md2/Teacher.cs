using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Teacher
{
    public int TeacherId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Patronymic { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; set; } = new List<CourseWork>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<Tsch> Tsches { get; set; } = new List<Tsch>();

    public virtual ICollection<Workload> Workloads { get; set; } = new List<Workload>();
}
