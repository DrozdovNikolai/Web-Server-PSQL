using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Contract
{
    public int Id { get; set; }

    public int? ListenerId { get; set; }

    public int? PayerId { get; set; }

    public int? ProgramId { get; set; }

    public DateOnly? CertDate { get; set; }

    public int? ListenedHours { get; set; }

    public DateOnly? DateEnroll { get; set; }

    public DateOnly? DateKick { get; set; }

    public int? GroupToMove { get; set; }

    public string? ContrNumber { get; set; }

    public virtual LGroup? GroupToMoveNavigation { get; set; }

    public virtual Listener? Listener { get; set; }
}
