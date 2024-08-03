using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Direction
{
    public int DirId { get; set; }

    public string? DirName { get; set; }

    public string? DirCode { get; set; }

    public bool? Magister { get; set; }

    public virtual ICollection<Group> Groups { get; } = new List<Group>();

    public virtual ICollection<Profile> Profiles { get; } = new List<Profile>();
}
