using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Configuration;

public sealed class KubernetesOptions
{
    public const string SectionName = "Kubernetes";

    public bool Enabled { get; init; }

    [Required]
    [StringLength(63)]
    [RegularExpression("^[a-z0-9]([-a-z0-9]*[a-z0-9])?$")]
    public string Namespace { get; init; } = "postgres-control";

    [Required]
    [StringLength(512)]
    public string WorkloadImage { get; init; } = "postgres-control-plane:0.1.0";

    public string? PublicBaseUrl { get; init; }

    [StringLength(253)]
    [RegularExpression("^[a-z0-9]([-.a-z0-9]*[a-z0-9])?$")]
    public string? IngressTlsSecretName { get; init; }
}
