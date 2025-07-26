namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record StoredFileResponse(
    string Id,
    long Size,
    string ContentType,
    DateTime CreatedAt,
    string DownloadUrl);

public sealed record StoredFilePageResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<StoredFileResponse> Items);
