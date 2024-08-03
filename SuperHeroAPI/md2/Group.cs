using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Group
{
    public int GroupId { get; set; }

    public int? GroupDirId { get; set; }

    public int? GroupProfId { get; set; }

    public string? GroupNumber { get; set; }

    public int? Course { get; set; }

    public bool? Magister { get; set; }

    public virtual Direction? GroupDir { get; set; }

    public virtual Profile? GroupProf { get; set; }

    public virtual ICollection<Schedule> Schedules { get; } = new List<Schedule>();

    public virtual ICollection<Student> Students { get; } = new List<Student>();

    public virtual ICollection<Workload> Workloads { get; } = new List<Workload>();
}
