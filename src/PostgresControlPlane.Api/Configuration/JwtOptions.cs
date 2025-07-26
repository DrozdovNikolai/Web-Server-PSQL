using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(5, 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}
