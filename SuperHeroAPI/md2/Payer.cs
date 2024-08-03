using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Payer
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string? Lastname { get; set; }

    public string? Snils { get; set; }

    public string? Passport { get; set; }

    public string? IssuedBy { get; set; }

    public DateOnly? IssueDate { get; set; }

    public string? DepartmentCode { get; set; }

    public string? RegistrationAddress { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Contract> Contracts { get; } = new List<Contract>();
}
