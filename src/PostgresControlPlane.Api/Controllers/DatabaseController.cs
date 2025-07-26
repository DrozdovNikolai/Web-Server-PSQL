using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;
using PostgresControlPlane.Api.Services.DatabaseRoutines;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/database")]
[Authorize(Policy = AuthorizationPolicies.DatabaseRead)]
[EnableRateLimiting(RateLimitPolicies.Database)]
public sealed partial class DatabaseController : ControllerBase
{
    private const int MaximumMetadataItems = 1000;
    private readonly DataContext _context;
    private readonly DatabaseBrowserOptions _options;
    private readonly IDatabaseRoutineService _routineService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        DataContext context,
        IOptions<DatabaseBrowserOptions> options,
        IDatabaseRoutineService routineService,
        ILogger<DatabaseController> logger)
    {
        _context = context;
        _options = options.Value;
        _routineService = routineService;
        _logger = logger;
    }

    [HttpGet("schemas")]
    [ProducesResponseType<IReadOnlyList<DatabaseSchemaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseSchemaResponse>>> GetSchemas(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name NOT IN ('pg_catalog', 'information_schema')
              AND schema_name <> 'control'
              AND schema_name NOT LIKE 'pg_toast%'
              AND schema_name NOT LIKE 'pg_temp_%'
            ORDER BY schema_name
            """;

        return Ok(await QueryAsync(
            sql,
            static reader => new DatabaseSchemaResponse(reader.GetString(0)),
            cancellationToken));
    }

    [HttpGet("tables")]
    [ProducesResponseType<IReadOnlyList<DatabaseTableResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseTableResponse>>> GetTables(
        [FromQuery] string schema = "public",
        CancellationToken cancellationToken = default)
    {
        if (!IsBrowsableSchema(schema))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid database identifier",
                detail: "Schema must be a valid PostgreSQL identifier.");
        }

        const string sql = """
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema = @schema
            ORDER BY table_name
            """;

        return Ok(await QueryAsync(
            sql,
            reader => new DatabaseTableResponse(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)),
            cancellationToken,
            new NpgsqlParameter("schema", schema)));
    }

    [HttpGet("tables/{schema}/{table}/rows")]
    [ProducesResponseType<TableRowsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TableRowsResponse>> GetRows(
        string schema,
        string table,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsBrowsableSchema(schema) || !IsValidIdentifier(table))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid database identifier",
                detail: "Schema and table must be valid PostgreSQL identifiers.");
        }

        if (page < 1 || pageSize < 1 || pageSize > _options.MaxPageSize)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination",
                detail: $"Page must be at least 1 and page size must be between 1 and {_options.MaxPageSize}.");
        }

        if (!await TableExistsAsync(schema, table, cancellationToken))
        {
            return NotFound();
        }

        var offset = checked((long)(page - 1) * pageSize);
        var sql = $"SELECT * FROM {QuoteIdentifier(schema)}.{QuoteIdentifier(table)} LIMIT @limit OFFSET @offset";
        var result = await QueryRowsAsync(
            sql,
            cancellationToken,
            new NpgsqlParameter<int>("limit", pageSize),
            new NpgsqlParameter<long>("offset", offset));

        return Ok(new TableRowsResponse(
            schema,
            table,
            page,
            pageSize,
            result.Rows,
            result.Truncated));
    }

    [HttpGet("routines")]
    [ProducesResponseType<IReadOnlyList<DatabaseRoutineResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseRoutineResponse>>> GetRoutines(
        [FromQuery] string schema = "public",
        CancellationToken cancellationToken = default)
    {
        if (!IsBrowsableSchema(schema))
        {
            return BadRequest();
        }

        return Ok(await _routineService.GetRoutinesAsync(schema, cancellationToken));
    }

    [HttpPost("routines/{schema}/{routineName}/invoke")]
    [Authorize(Policy = AuthorizationPolicies.DatabaseExecute)]
    [RequestSizeLimit(1024 * 1024)]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> InvokeRoutine(
        string schema,
        string routineName,
        RoutineInvocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsBrowsableSchema(schema) || !IsValidIdentifier(routineName))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid routine address");
        }

        var result = await _routineService.InvokeAsync(
            schema,
            routineName,
            request.IdentityArguments,
            request.Arguments,
            cancellationToken);

        return result.Status switch
        {
            RoutineInvocationStatus.Success => CreateRoutineResult(result.Response!),
            RoutineInvocationStatus.Disabled => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Routine execution is disabled"),
            RoutineInvocationStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Routine not found"),
            RoutineInvocationStatus.Ambiguous => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Routine name is ambiguous",
                detail: result.Detail),
            RoutineInvocationStatus.NotInvocable => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Routine cannot be invoked",
                detail: result.Detail),
            RoutineInvocationStatus.InvalidArguments => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid routine arguments",
                detail: result.Detail),
            RoutineInvocationStatus.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Routine execution is not permitted"),
            RoutineInvocationStatus.TimedOut => Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Routine execution timed out"),
            _ => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Routine execution failed",
                detail: "PostgreSQL rejected the invocation. Verify the argument values and routine preconditions.")
        };
    }

    [HttpPost("routines/{schema}/{routineName}/export/xlsx")]
    [Authorize(Policy = AuthorizationPolicies.DatabaseExecute)]
    [RequestSizeLimit(1024 * 1024)]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/problem+json")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<IActionResult> ExportRoutineToExcel(
        string schema,
        string routineName,
        RoutineExcelExportRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsBrowsableSchema(schema) || !IsValidIdentifier(routineName))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid routine address");
        }

        var result = await _routineService.ExportCursorToExcelAsync(
            schema,
            routineName,
            request.IdentityArguments,
            request.Arguments,
            request.Excel,
            cancellationToken);

        return result.Status switch
        {
            RoutineExcelExportStatus.Success => File(
                result.File!.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.File.FileName),
            RoutineExcelExportStatus.Disabled => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Excel export is disabled"),
            RoutineExcelExportStatus.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Routine not found"),
            RoutineExcelExportStatus.Ambiguous => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Routine name is ambiguous",
                detail: result.Detail),
            RoutineExcelExportStatus.NotInvocable => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Routine cannot be invoked",
                detail: result.Detail),
            RoutineExcelExportStatus.NotCursor => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Routine cannot be exported",
                detail: result.Detail),
            RoutineExcelExportStatus.InvalidArguments => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid routine arguments",
                detail: result.Detail),
            RoutineExcelExportStatus.InvalidSettings => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Excel settings",
                detail: result.Detail),
            RoutineExcelExportStatus.TooLarge => Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Excel report is too large",
                detail: result.Detail),
            RoutineExcelExportStatus.Forbidden => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Routine execution is not permitted"),
            RoutineExcelExportStatus.TimedOut => Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Routine execution timed out"),
            _ => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Excel export failed",
                detail: "PostgreSQL rejected the cursor invocation or the workbook could not be generated.")
        };
    }

    private IActionResult CreateRoutineResult(RoutineInvocationResponse response)
    {
        Response.Headers["Server-Timing"] =
            $"postgres-routine;dur={response.DurationMilliseconds}";
        if (response.Truncated)
        {
            Response.Headers["X-Result-Truncated"] = "true";
        }

        if (response.Type == "PROCEDURE" && response.Columns.Count == 0)
        {
            return NoContent();
        }

        object? payload;
        if (response.ReturnsSet)
        {
            payload = response.Columns.Count == 1
                ? response.Rows.Select(row => row[response.Columns[0]]).ToArray()
                : response.Rows;
        }
        else if (response.Rows.Count == 0)
        {
            payload = null;
        }
        else if (response.Type == "FUNCTION" && response.Columns.Count == 1)
        {
            payload = response.Rows[0][response.Columns[0]];
        }
        else
        {
            payload = response.Rows[0];
        }

        return new JsonResult(payload) { StatusCode = StatusCodes.Status200OK };
    }

    [HttpGet("triggers")]
    [ProducesResponseType<IReadOnlyList<DatabaseTriggerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DatabaseTriggerResponse>>> GetTriggers(
        [FromQuery] string schema = "public",
        CancellationToken cancellationToken = default)
    {
        if (!IsBrowsableSchema(schema))
        {
            return BadRequest();
        }

        const string sql = """
            SELECT trigger_schema, trigger_name, event_object_table, event_manipulation, action_timing
            FROM information_schema.triggers
            WHERE trigger_schema = @schema
            ORDER BY trigger_name, event_manipulation
            """;

        return Ok(await QueryAsync(
            sql,
            reader => new DatabaseTriggerResponse(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)),
            cancellationToken,
            new NpgsqlParameter("schema", schema)));
    }

    private async Task<bool> TableExistsAsync(
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table
            )
            """;

        await using var command = CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter("schema", schema));
        command.Parameters.Add(new NpgsqlParameter("table", table));
        await EnsureConnectionOpenAsync(command.Connection!, cancellationToken);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        try
        {
            await using var command = CreateCommand(sql);
            command.Parameters.AddRange(parameters);
            await EnsureConnectionOpenAsync(command.Connection!, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            var result = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(map(reader));
                if (result.Count >= MaximumMetadataItems)
                {
                    break;
                }
            }

            return result;
        }
        catch (PostgresException exception)
        {
            LogDatabaseBrowserFailure(_logger, exception.SqlState, exception);
            throw new InvalidOperationException("The database browser operation could not be completed.", exception);
        }
    }

    private async Task<RowQueryResult> QueryRowsAsync(
        string sql,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        try
        {
            await using var command = CreateCommand(sql);
            command.Parameters.AddRange(parameters);
            await EnsureConnectionOpenAsync(command.Connection!, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            var rows = new List<IReadOnlyDictionary<string, object?>>();
            var estimatedBytes = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    var value = ReadBoundedValue(reader, index);
                    estimatedBytes += reader.GetName(index).Length * sizeof(char) + EstimateSize(value);
                    if (estimatedBytes > _options.MaxResponseBytes)
                    {
                        return new RowQueryResult(rows, Truncated: true);
                    }

                    row[reader.GetName(index)] = value;
                }

                rows.Add(row);
            }

            return new RowQueryResult(rows, Truncated: false);
        }
        catch (PostgresException exception)
        {
            LogDatabaseBrowserFailure(_logger, exception.SqlState, exception);
            throw new InvalidOperationException("The database browser operation could not be completed.", exception);
        }
    }

    private object? ReadBoundedValue(DbDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        return value switch
        {
            string text when text.Length > _options.MaxTextLength =>
                string.Concat(text.AsSpan(0, _options.MaxTextLength), "…"),
            byte[] bytes when bytes.Length > _options.MaxTextLength =>
                $"{Convert.ToBase64String(bytes, 0, _options.MaxTextLength)}…",
            byte[] bytes => Convert.ToBase64String(bytes),
            Array array when array.Length > 100 => $"{array.GetType().GetElementType()?.Name ?? "value"}[{array.Length}]",
            _ => value
        };
    }

    private static int EstimateSize(object? value)
    {
        return value switch
        {
            null => 4,
            string text => text.Length * sizeof(char),
            byte[] bytes => bytes.Length,
            _ => 32
        };
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = _options.CommandTimeoutSeconds;
        return command;
    }

    private static async Task EnsureConnectionOpenAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static bool IsValidIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 63 &&
        IdentifierPattern().IsMatch(value);

    private static bool IsBrowsableSchema(string value) =>
        IsValidIdentifier(value) &&
        !string.Equals(value, "control", StringComparison.Ordinal);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_$]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierPattern();

    [LoggerMessage(1, LogLevel.Warning, "A database browser operation failed with SQLSTATE {SqlState}")]
    private static partial void LogDatabaseBrowserFailure(
        ILogger logger,
        string sqlState,
        Exception exception);

    private sealed record RowQueryResult(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
        bool Truncated);
}
