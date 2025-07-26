using System.ComponentModel.DataAnnotations;

namespace PostgresControlPlane.Api.Configuration;

public sealed class DatabaseBrowserOptions
{
    public const string SectionName = "DatabaseBrowser";

    [Range(1, 30)]
    public int CommandTimeoutSeconds { get; init; } = 5;

    [Range(1, 500)]
    public int MaxPageSize { get; init; } = 200;

    [Range(256, 65_536)]
    public int MaxTextLength { get; init; } = 4096;

    [Range(64 * 1024, 10 * 1024 * 1024)]
    public int MaxResponseBytes { get; init; } = 1024 * 1024;

    public bool RoutineExecutionEnabled { get; init; }

    public string[] AllowedRoutineSchemas { get; init; } = [];

    public bool AllowProcedures { get; init; } = true;

    public bool AllowSecurityDefiner { get; init; }

    [Range(1, 30)]
    public int RoutineCommandTimeoutSeconds { get; init; } = 10;

    [Range(1, 64)]
    public int MaxRoutineArguments { get; init; } = 32;

    [Range(64, 1024 * 1024)]
    public int MaxRoutineArgumentLength { get; init; } = 16_384;

    [Range(1, 1000)]
    public int MaxRoutineRows { get; init; } = 200;

    public bool RoutineExcelExportEnabled { get; init; }

    [Range(1, 100_000)]
    public int MaxExcelRows { get; init; } = 10_000;

    [Range(1, 16_384)]
    public int MaxExcelColumns { get; init; } = 256;

    [Range(1, 1_000_000)]
    public int MaxExcelCells { get; init; } = 200_000;

    [Range(64 * 1024, 100 * 1024 * 1024)]
    public int MaxExcelFileSizeBytes { get; init; } = 10 * 1024 * 1024;
}
