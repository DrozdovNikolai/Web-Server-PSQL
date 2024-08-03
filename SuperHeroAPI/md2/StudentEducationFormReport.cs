using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class StudentEducationFormReport
{
    public int ReportId { get; set; }

    public string? ReportContent { get; set; }

    public DateOnly? ReportDate { get; set; }

    public string? EducationForm { get; set; }

    public virtual ICollection<Student> Students { get; } = new List<Student>();
}
