using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record RoleResponse(int Id, string Name);

public sealed record CreateRoleRequest(
    [Required]
    [RegularExpression("^[a-z][a-z0-9_-]{2,31}$")]
    string Name);

public sealed record UpdateRoleRequest(
    [Required]
    [RegularExpression("^[a-z][a-z0-9_-]{2,31}$")]
    string Name);
