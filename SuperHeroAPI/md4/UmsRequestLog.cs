using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md4;

public partial class UmsRequestLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Path { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string QueryString { get; set; } = null!;

    public string RequestBody { get; set; } = null!;

    public string ResponseBody { get; set; } = null!;

    public int StatusCode { get; set; }

    public DateTime RequestTime { get; set; }

    public DateTime ResponseTime { get; set; }

    public TimeSpan Duration { get; set; }

    public string IpAddress { get; set; } = null!;

    public virtual UmsUser? User { get; set; }
}
