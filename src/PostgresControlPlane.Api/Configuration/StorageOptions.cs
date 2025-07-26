using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string RootPath { get; init; } = "data/uploads";

    [Range(1, 100 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
}
