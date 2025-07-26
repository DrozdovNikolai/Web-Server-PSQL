namespace PostgresControlPlane.Api.Configuration;

public sealed class ProxyOptions
{
    public const string SectionName = "Proxy";

    public string[] KnownProxies { get; init; } = [];
}
