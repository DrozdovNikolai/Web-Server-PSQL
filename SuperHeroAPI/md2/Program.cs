using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Program
{
    public int Id { get; set; }

    public decimal RequiredAmount { get; set; }

    public string ProgramName { get; set; } = null!;

    public int Hours { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
