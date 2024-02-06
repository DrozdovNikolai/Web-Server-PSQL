using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int? AtStudentId { get; set; }

    public int? AtTegrsuId { get; set; }

    public DateOnly? AtDate { get; set; }

    public int? Status { get; set; }
}
