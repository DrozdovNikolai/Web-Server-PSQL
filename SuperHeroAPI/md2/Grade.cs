using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Grade
{
    public int GradeId { get; set; }

    public int? GrStudentId { get; set; }

    public int? GrTegrsuId { get; set; }

    public DateOnly? GrDate { get; set; }

    public int? Grade1 { get; set; }
}
