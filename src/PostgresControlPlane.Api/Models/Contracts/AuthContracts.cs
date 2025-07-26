using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record LoginRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, StringLength(200, MinimumLength = 8)] string Password);

public sealed record RegisterUserRequest(
    [Required, StringLength(100, MinimumLength = 3)] string Username,
    [Required, StringLength(200, MinimumLength = 12)] string Password);

public sealed record UpdateCurrentUserRequest(
    [Required, StringLength(200, MinimumLength = 8)] string CurrentPassword,
    [StringLength(100, MinimumLength = 3)] string? Username,
    [StringLength(200, MinimumLength = 12)] string? NewPassword);

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTime ExpiresAt,
    CurrentUserResponse User);

public sealed record CurrentUserResponse(
    int Id,
    string Username,
    IReadOnlyList<string> Roles);

public sealed record SessionResponse(
    int Id,
    int UserId,
    string Username,
    DateTime ExpiresAt);

public sealed record SessionPageResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<SessionResponse> Items);
