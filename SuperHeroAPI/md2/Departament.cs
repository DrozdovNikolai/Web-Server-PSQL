using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Departament
{
    public int DepId { get; set; }

    public string? DepName { get; set; }

    public string? DepAbb { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; set; } = new List<CourseWork>();
}
