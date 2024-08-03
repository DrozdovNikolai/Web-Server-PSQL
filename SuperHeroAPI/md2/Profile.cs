using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class Profile
{
    public int ProfId { get; set; }

    public int? ProfDirId { get; set; }

    public string? ProfName { get; set; }

    public virtual ICollection<Group> Groups { get; } = new List<Group>();

    public virtual Direction? ProfDir { get; set; }
}
