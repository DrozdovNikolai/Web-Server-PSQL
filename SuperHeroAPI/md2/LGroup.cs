using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class LGroup
{
    public int Id { get; set; }

    public int? GroupProgramId { get; set; }

    public int? Hours { get; set; }

    public string? GroupNumber { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? Starttime { get; set; }

    public TimeOnly? Endtime { get; set; }

    public int? PeopleCount { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<LGroupsDay> LGroupsDays { get; set; } = new List<LGroupsDay>();
}
