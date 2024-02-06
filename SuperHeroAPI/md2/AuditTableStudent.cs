using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class AuditTableStudent
{
    public int AuditId { get; set; }

    public int? StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Patronymic { get; set; }

    public char? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PassportSeriesAndNumber { get; set; }

    public string? Inn { get; set; }

    public string? Snils { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? Email { get; set; }

    public string? StudentLogin { get; set; }

    public string? EnrollmentOrder { get; set; }

    public DateOnly? EnrolledDate { get; set; }

    public int? Course { get; set; }

    public int? CourseWorkId { get; set; }

    public int? GroupId { get; set; }

    public char? Operation { get; set; }

    public DateTime? Timestamp { get; set; }
}
