using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Journal
{
    public int JId { get; set; }

    public DateOnly? Date { get; set; }

    public int? Grade { get; set; }

    public string? Status { get; set; }

    public int? StudentId { get; set; }

    public int? TeacherId { get; set; }

    public int? SubjectId { get; set; }

    public int? GroupId { get; set; }
}
