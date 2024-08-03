using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class LWishDay
{
    public int LWishDayId { get; set; }

    public int? DayId { get; set; }

    public TimeOnly? Starttime { get; set; }

    public TimeOnly? Endtime { get; set; }

    public int? ListenerId { get; set; }

    public virtual Listener? Listener { get; set; }
}
