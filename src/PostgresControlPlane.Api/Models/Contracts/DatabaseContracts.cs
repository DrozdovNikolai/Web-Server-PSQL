using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PostgresControlPlane.Api.Models.Contracts;

public sealed record DatabaseSchemaResponse(string Name);

public sealed record DatabaseTableResponse(string Schema, string Name, string Type);

public sealed record DatabaseRoutineArgumentResponse(
    int Position,
    string Name,
    string Mode,
    string DataType,
    bool Required);

public sealed record DatabaseRoutineResponse(
    string Schema,
    string Name,
    string Type,
    string Signature,
    string IdentityArguments,
    string? ReturnType,
    bool ReturnsSet,
    bool ReturnsCursor,
    string Volatility,
    bool SecurityDefiner,
    bool CanInvoke,
    string? InvocationDisabledReason,
    IReadOnlyList<DatabaseRoutineArgumentResponse> Arguments);

public sealed class RoutineInvocationRequest
{
    [MaxLength(1024)]
    public string? IdentityArguments { get; init; }

    [Required]
    public IReadOnlyList<JsonElement> Arguments { get; init; } = [];
}

public sealed record RoutineInvocationResponse(
    string Type,
    bool ReturnsSet,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    bool Truncated,
    long DurationMilliseconds);

public sealed class RoutineExcelExportRequest
{
    [MaxLength(1024)]
    public string? IdentityArguments { get; init; }

    [Required]
    public IReadOnlyList<JsonElement> Arguments { get; init; } = [];

    [Required]
    public ExcelExportSettings Excel { get; init; } = new();
}

public sealed class ExcelExportSettings
{
    [StringLength(120, MinimumLength = 1)]
    public string FileName { get; init; } = "report.xlsx";

    [StringLength(31, MinimumLength = 1)]
    public string SheetName { get; init; } = "Report";

    public ExcelReportStartPosition ReportStartPosition { get; init; } = new();

    public bool AutoFitColumns { get; init; } = true;

    [MaxLength(100)]
    public IReadOnlyList<ExcelRangeSettings> Ranges { get; init; } = [];

    [MaxLength(200)]
    public IReadOnlyList<ExcelCellSettings> Cells { get; init; } = [];

    public IReadOnlyDictionary<int, double> ColumnWidths { get; init; } =
        new Dictionary<int, double>();

    public IReadOnlyDictionary<int, double> RowHeights { get; init; } =
        new Dictionary<int, double>();

    public ExcelFreezePaneSettings FreezePane { get; init; } = new();
}

public sealed class ExcelReportStartPosition
{
    [Range(1, 1_048_576)]
    public int Row { get; init; } = 1;

    [Range(1, 16_384)]
    public int Column { get; init; } = 1;
}

public sealed class ExcelRangeSettings
{
    [Range(0, 1_048_575)]
    public int Top { get; init; }

    [Range(0, 16_383)]
    public int Left { get; init; }

    [Range(0, 1_048_575)]
    public int Bottom { get; init; }

    [Range(0, 16_383)]
    public int Right { get; init; }

    public bool Merge { get; init; }

    public ExcelStyleSettings? Style { get; init; }
}

public sealed class ExcelCellSettings
{
    [Range(0, 1_048_575)]
    public int Row { get; init; }

    [Range(0, 16_383)]
    public int Column { get; init; }

    public JsonElement? Value { get; init; }

    [RegularExpression("^(auto|string|number|boolean|date)$")]
    public string ValueType { get; init; } = "auto";

    public ExcelStyleSettings? Style { get; init; }
}

public sealed class ExcelStyleSettings
{
    [MaxLength(100)]
    public string? NumberFormat { get; init; }

    [RegularExpression("^#?[0-9A-Fa-f]{6}$")]
    public string? FillColor { get; init; }

    [RegularExpression("^#?[0-9A-Fa-f]{6}$")]
    public string? FontColor { get; init; }

    [MaxLength(50)]
    public string? FontName { get; init; }

    [Range(6, 72)]
    public double? FontSize { get; init; }

    public bool? Bold { get; init; }

    [RegularExpression("^(left|center|right|justify)$")]
    public string? HorizontalAlignment { get; init; }

    [RegularExpression("^(top|center|bottom|justify)$")]
    public string? VerticalAlignment { get; init; }

    public bool Border { get; init; }

    [RegularExpression("^#?[0-9A-Fa-f]{6}$")]
    public string? BorderColor { get; init; }
}

public sealed class ExcelFreezePaneSettings
{
    [Range(0, 1_048_575)]
    public int Rows { get; init; }

    [Range(0, 16_383)]
    public int Columns { get; init; }
}

public sealed record DatabaseTriggerResponse(
    string Schema,
    string Name,
    string Table,
    string Event,
    string Timing);

public sealed record TableRowsResponse(
    string Schema,
    string Table,
    int Page,
    int PageSize,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
    bool Truncated);
