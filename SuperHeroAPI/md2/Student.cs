using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Student
{
    public int StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Patronymic { get; set; }

    public string? Gender { get; set; }

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

    public string? PhoneNumber { get; set; }

    public string? PhoneNumberRod { get; set; }

    public string? ZachetkaNumber { get; set; }

    public string? Subgroup { get; set; }

    public bool? IsBudget { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; } = new List<CourseWork>();

    public virtual Group? Group { get; set; }

    public virtual ICollection<StudentEducationFormReport> Reports { get; } = new List<StudentEducationFormReport>();
}
