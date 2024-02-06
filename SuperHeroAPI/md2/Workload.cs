using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Workload
{
    public int WlId { get; set; }

    public int GroupId { get; set; }

    public int SubjectId { get; set; }

    public int TeacherId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;

    public virtual ICollection<Teachschedule> Teachschedules { get; set; } = new List<Teachschedule>();
}
