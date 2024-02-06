using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class LGroupsDay
{
    public int LGroupsDaysId { get; set; }

    public int? DayId { get; set; }

    public TimeOnly? Starttime { get; set; }

    public TimeOnly? Endtime { get; set; }

    public int? LGroups { get; set; }

    public virtual LGroup? LGroupsNavigation { get; set; }
}
