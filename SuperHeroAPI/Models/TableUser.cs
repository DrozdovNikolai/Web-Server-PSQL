﻿using System;
using System.Collections.Generic;

namespace SuperHeroAPI.Models;

public partial class TableUser
{
    public int Id { get; set; }

    public string? Tablename { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
