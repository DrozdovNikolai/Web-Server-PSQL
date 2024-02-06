using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Discipline
{
    public int DisciplinesId { get; set; }

    public int GroupNumber { get; set; }

    public string DisciplineName { get; set; } = null!;
}
