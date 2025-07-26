using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record UserSummaryResponse(
    int Id,
    string Username,
    IReadOnlyList<string> Roles);

public sealed record UserPageResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<UserSummaryResponse> Items);

public sealed record AdminUpdateUserRequest(
    [StringLength(100, MinimumLength = 3)] string? Username,
    [StringLength(200, MinimumLength = 12)] string? NewPassword,
    IReadOnlyList<int>? RoleIds);
