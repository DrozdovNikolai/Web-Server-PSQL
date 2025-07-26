using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record DeploymentRequest(
    [Required, StringLength(63), RegularExpression("^[a-z0-9]([-a-z0-9]*[a-z0-9])?$")]
    string Name,
    [Required, StringLength(255)] string DatabaseHost,
    [Range(1, 65_535)] int DatabasePort,
    [Required, StringLength(255)] string DatabaseName,
    [Required, StringLength(255)] string DatabaseUser,
    [Required, StringLength(255, MinimumLength = 12)] string DatabasePassword,
    [Required, StringLength(100, MinimumLength = 3), RegularExpression("^[A-Za-z0-9_.-]+$")]
    string ApplicationAdminUsername,
    [Required, StringLength(255, MinimumLength = 12)] string ApplicationAdminPassword);

public sealed record DeploymentResponse(
    int Id,
    string Name,
    string Status,
    string DatabaseHost,
    int DatabasePort,
    string DatabaseName,
    string ExternalUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record DeploymentPageResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<DeploymentResponse> Items);
