using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Services.DatabaseRoutines;

public interface IDatabaseRoutineService
{
    Task<IReadOnlyList<DatabaseRoutineResponse>> GetRoutinesAsync(
        string schema,
        CancellationToken cancellationToken);

    Task<RoutineInvocationResult> InvokeAsync(
        string schema,
        string routineName,
        string? identityArguments,
        IReadOnlyList<System.Text.Json.JsonElement> arguments,
        CancellationToken cancellationToken);

    Task<RoutineExcelExportResult> ExportCursorToExcelAsync(
        string schema,
        string routineName,
        string? identityArguments,
        IReadOnlyList<System.Text.Json.JsonElement> arguments,
        ExcelExportSettings settings,
        CancellationToken cancellationToken);
}

public enum RoutineInvocationStatus
{
    Success,
    Disabled,
    NotFound,
    Ambiguous,
    NotInvocable,
    InvalidArguments,
    Forbidden,
    TimedOut,
    Failed
}

public sealed record RoutineInvocationResult(
    RoutineInvocationStatus Status,
    RoutineInvocationResponse? Response = null,
    string? Detail = null);

public enum RoutineExcelExportStatus
{
    Success,
    Disabled,
    NotFound,
    Ambiguous,
    NotInvocable,
    NotCursor,
    InvalidArguments,
    InvalidSettings,
    TooLarge,
    Forbidden,
    TimedOut,
    Failed
}

public sealed record RoutineExcelFile(byte[] Content, string FileName);

public sealed record RoutineExcelExportResult(
    RoutineExcelExportStatus Status,
    RoutineExcelFile? File = null,
    string? Detail = null);
