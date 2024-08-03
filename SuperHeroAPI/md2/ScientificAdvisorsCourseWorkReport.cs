using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class ScientificAdvisorsCourseWorkReport
{
    public int ReportId { get; set; }

    public string? ReportContent { get; set; }

    public DateOnly? ReportDate { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; } = new List<CourseWork>();
}
