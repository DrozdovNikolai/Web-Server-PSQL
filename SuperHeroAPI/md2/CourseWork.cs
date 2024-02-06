using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class CourseWork
{
    public int CourseWorkId { get; set; }

    public int? CourseWorkTeacherId { get; set; }

    public string? CourseWorkTheme { get; set; }

    public int? CourseWorkStudentId { get; set; }

    public int? CourseWorkKafedra { get; set; }

    public int? CourseWorkOcenka { get; set; }

    public int? CourseWorkYear { get; set; }

    public bool? CourseWorkVipysk { get; set; }

    public virtual Departament? CourseWorkKafedraNavigation { get; set; }

    public virtual Student? CourseWorkStudent { get; set; }

    public virtual Teacher? CourseWorkTeacher { get; set; }
}
