using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class ListenerWish
{
    public int WishId { get; set; }

    public int? PeopleCount { get; set; }

    public int? Hours { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? ListenerId { get; set; }

    public string? WishDescription { get; set; }

    public int[]? SuitableDays { get; set; }
}
