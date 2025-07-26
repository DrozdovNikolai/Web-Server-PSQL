using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Data;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Services.DatabaseRoutines;

public sealed partial class DatabaseRoutineService : IDatabaseRoutineService
{
    private const int MaximumCatalogItems = 1000;
    private const string CatalogSelect = """
        SELECT p.oid::bigint,
               n.nspname,
               p.proname,
               CASE p.prokind WHEN 'p' THEN 'PROCEDURE' ELSE 'FUNCTION' END,
               pg_catalog.pg_get_function_identity_arguments(p.oid),
               pg_catalog.oidvectortypes(p.proargtypes),
               pg_catalog.pg_get_function_result(p.oid),
               p.proretset,
               CASE p.provolatile
                   WHEN 'i' THEN 'IMMUTABLE'
                   WHEN 's' THEN 'STABLE'
                   ELSE 'VOLATILE'
               END,
               p.prosecdef,
               p.pronargdefaults,
               p.provariadic <> 0,
               pg_catalog.has_function_privilege(p.oid, 'EXECUTE'),
               return_type_namespace.nspname,
               return_type.typname,
               argument.ordinal_position::integer,
               p.proargnames[argument.ordinal_position::integer],
               COALESCE(p.proargmodes[argument.ordinal_position::integer], 'i')::text,
               type_namespace.nspname,
               argument_type.typname,
               pg_catalog.format_type(argument.type_oid, NULL),
               argument_type.typtype::text,
               argument_type.typcategory::text
        FROM pg_catalog.pg_proc AS p
        INNER JOIN pg_catalog.pg_namespace AS n ON n.oid = p.pronamespace
        INNER JOIN pg_catalog.pg_type AS return_type ON return_type.oid = p.prorettype
        INNER JOIN pg_catalog.pg_namespace AS return_type_namespace
            ON return_type_namespace.oid = return_type.typnamespace
        LEFT JOIN LATERAL unnest(
            COALESCE(p.proallargtypes, p.proargtypes::oid[])
        ) WITH ORDINALITY AS argument(type_oid, ordinal_position) ON TRUE
        LEFT JOIN pg_catalog.pg_type AS argument_type ON argument_type.oid = argument.type_oid
        LEFT JOIN pg_catalog.pg_namespace AS type_namespace ON type_namespace.oid = argument_type.typnamespace
        WHERE p.prokind IN ('f', 'p')
          AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'control')
        """;

    private readonly DataContext _context;
    private readonly DatabaseBrowserOptions _options;
    private readonly IExcelReportBuilder _excelReportBuilder;
    private readonly ILogger<DatabaseRoutineService> _logger;

    public DatabaseRoutineService(
        DataContext context,
        IOptions<DatabaseBrowserOptions> options,
        IExcelReportBuilder excelReportBuilder,
        ILogger<DatabaseRoutineService> logger)
    {
        _context = context;
        _options = options.Value;
        _excelReportBuilder = excelReportBuilder;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DatabaseRoutineResponse>> GetRoutinesAsync(
        string schema,
        CancellationToken cancellationToken)
    {
        var routines = await QueryCatalogAsync(
            "AND n.nspname = @schema",
            cancellationToken,
            new NpgsqlParameter("schema", schema));

        return routines.Select(ToResponse).ToArray();
    }

    public async Task<RoutineInvocationResult> InvokeAsync(
        string schema,
        string routineName,
        string? identityArguments,
        IReadOnlyList<JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        if (!_options.RoutineExecutionEnabled)
        {
            return new(RoutineInvocationStatus.Disabled);
        }

        if (arguments.Count > _options.MaxRoutineArguments)
        {
            return new(
                RoutineInvocationStatus.InvalidArguments,
                Detail: $"At most {_options.MaxRoutineArguments} arguments are allowed.");
        }

        var resolution = await ResolveRoutineAsync(
            schema,
            routineName,
            identityArguments,
            cancellationToken);
        if (resolution.Status == RoutineResolutionStatus.NotFound)
        {
            return new(RoutineInvocationStatus.NotFound);
        }
        if (resolution.Status == RoutineResolutionStatus.Ambiguous)
        {
            return new(RoutineInvocationStatus.Ambiguous, Detail: resolution.Detail);
        }

        var routine = resolution.Routine!;

        var disabledReason = GetInvocationDisabledReason(routine);
        if (disabledReason is not null)
        {
            return new(RoutineInvocationStatus.NotInvocable, Detail: disabledReason);
        }

        var preparation = PrepareInvocation(routine, arguments);
        if (preparation.Detail is not null)
        {
            return new(RoutineInvocationStatus.InvalidArguments, Detail: preparation.Detail);
        }

        try
        {
            var response = routine.ReturnsCursor
                ? await ExecuteCursorAsync(
                    routine,
                    preparation.InputArguments,
                    preparation.Values,
                    _options.MaxRoutineRows,
                    _options.MaxTextLength,
                    _options.MaxResponseBytes,
                    cancellationToken)
                : await ExecuteAsync(
                    routine,
                    preparation.InputArguments,
                    preparation.Values,
                    cancellationToken);
            return new(RoutineInvocationStatus.Success, response);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineInvocationStatus.Forbidden);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.QueryCanceled)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineInvocationStatus.TimedOut);
        }
        catch (NpgsqlException exception) when (exception.InnerException is TimeoutException)
        {
            LogInvocationFailure(_logger, routine.Id, "timeout");
            return new(RoutineInvocationStatus.TimedOut);
        }
        catch (PostgresException exception)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineInvocationStatus.Failed);
        }
        catch (InvalidOperationException)
        {
            LogInvocationFailure(_logger, routine.Id, "routine-contract");
            return new(RoutineInvocationStatus.Failed);
        }
    }

    public async Task<RoutineExcelExportResult> ExportCursorToExcelAsync(
        string schema,
        string routineName,
        string? identityArguments,
        IReadOnlyList<JsonElement> arguments,
        ExcelExportSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_options.RoutineExecutionEnabled || !_options.RoutineExcelExportEnabled)
        {
            return new(RoutineExcelExportStatus.Disabled);
        }

        if (arguments.Count > _options.MaxRoutineArguments)
        {
            return new(
                RoutineExcelExportStatus.InvalidArguments,
                Detail: $"At most {_options.MaxRoutineArguments} arguments are allowed.");
        }

        var resolution = await ResolveRoutineAsync(
            schema,
            routineName,
            identityArguments,
            cancellationToken);
        if (resolution.Status == RoutineResolutionStatus.NotFound)
        {
            return new(RoutineExcelExportStatus.NotFound);
        }
        if (resolution.Status == RoutineResolutionStatus.Ambiguous)
        {
            return new(RoutineExcelExportStatus.Ambiguous, Detail: resolution.Detail);
        }

        var routine = resolution.Routine!;
        var disabledReason = GetInvocationDisabledReason(routine);
        if (disabledReason is not null)
        {
            return new(RoutineExcelExportStatus.NotInvocable, Detail: disabledReason);
        }
        if (!routine.ReturnsCursor)
        {
            return new(
                RoutineExcelExportStatus.NotCursor,
                Detail: "Excel export requires a function returning pg_catalog.refcursor.");
        }

        var preparation = PrepareInvocation(routine, arguments);
        if (preparation.Detail is not null)
        {
            return new(RoutineExcelExportStatus.InvalidArguments, Detail: preparation.Detail);
        }

        try
        {
            var sourceByteLimit = checked(_options.MaxExcelFileSizeBytes * 4);
            var response = await ExecuteCursorAsync(
                routine,
                preparation.InputArguments,
                preparation.Values,
                _options.MaxExcelRows,
                32_767,
                sourceByteLimit,
                cancellationToken);
            var cellCount = (long)response.Rows.Count * response.Columns.Count;
            if (response.Truncated ||
                response.Columns.Count > _options.MaxExcelColumns ||
                cellCount > _options.MaxExcelCells)
            {
                return new(
                    RoutineExcelExportStatus.TooLarge,
                    Detail: "The cursor result exceeds the configured Excel row, column, cell or source-size limit.");
            }

            var file = _excelReportBuilder.Build(response, settings);
            if (file.Content.Length > _options.MaxExcelFileSizeBytes)
            {
                return new(
                    RoutineExcelExportStatus.TooLarge,
                    Detail: "The generated workbook exceeds the configured file-size limit.");
            }

            return new(RoutineExcelExportStatus.Success, file);
        }
        catch (ArgumentException exception)
        {
            return new(RoutineExcelExportStatus.InvalidSettings, Detail: exception.Message);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineExcelExportStatus.Forbidden);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.QueryCanceled)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineExcelExportStatus.TimedOut);
        }
        catch (NpgsqlException exception) when (exception.InnerException is TimeoutException)
        {
            LogInvocationFailure(_logger, routine.Id, "timeout");
            return new(RoutineExcelExportStatus.TimedOut);
        }
        catch (PostgresException exception)
        {
            LogInvocationFailure(_logger, routine.Id, exception.SqlState);
            return new(RoutineExcelExportStatus.Failed);
        }
        catch (InvalidOperationException)
        {
            LogInvocationFailure(_logger, routine.Id, "routine-contract");
            return new(RoutineExcelExportStatus.Failed);
        }
    }

    private async Task<RoutineResolution> ResolveRoutineAsync(
        string schema,
        string routineName,
        string? identityArguments,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(schema) || schema.Length > 63 ||
            string.IsNullOrWhiteSpace(routineName) || routineName.Length > 63 ||
            identityArguments?.Length > 1024)
        {
            return new(RoutineResolutionStatus.NotFound);
        }

        var hasIdentity = !string.IsNullOrWhiteSpace(identityArguments);
        var predicate = hasIdentity
            ? "AND n.nspname = @schema AND p.proname = @routine_name " +
              "AND pg_catalog.oidvectortypes(p.proargtypes) = @identity_arguments"
            : "AND n.nspname = @schema AND p.proname = @routine_name";
        var parameters = new List<NpgsqlParameter>
        {
            new("schema", schema),
            new("routine_name", routineName)
        };
        if (hasIdentity)
        {
            parameters.Add(new NpgsqlParameter("identity_arguments", identityArguments!.Trim()));
        }

        var routines = await QueryCatalogAsync(
            predicate,
            cancellationToken,
            parameters.ToArray());
        return routines.Count switch
        {
            0 => new(RoutineResolutionStatus.NotFound),
            1 => new(RoutineResolutionStatus.Success, routines[0]),
            _ => new(
                RoutineResolutionStatus.Ambiguous,
                Detail: "The routine name is overloaded. Send identityArguments from the catalog response.")
        };
    }

    private PreparedInvocation PrepareInvocation(
        RoutineDefinition routine,
        IReadOnlyList<JsonElement> arguments)
    {
        var inputArguments = routine.Arguments
            .Where(argument => argument.Mode is "i" or "b")
            .ToArray();
        var requiredArguments = Math.Max(0, inputArguments.Length - routine.DefaultArgumentCount);
        if (arguments.Count < requiredArguments || arguments.Count > inputArguments.Length)
        {
            return new(
                inputArguments,
                [],
                $"The routine expects between {requiredArguments} and {inputArguments.Length} positional arguments.");
        }

        var convertedArguments = new List<object?>(arguments.Count);
        for (var index = 0; index < arguments.Count; index++)
        {
            if (!TryConvertArgument(arguments[index], inputArguments[index], out var value))
            {
                return new(
                    inputArguments,
                    [],
                    $"Argument {index + 1} is not valid for PostgreSQL type {inputArguments[index].DisplayType}.");
            }

            convertedArguments.Add(value);
        }

        return new(inputArguments, convertedArguments);
    }

    private async Task<IReadOnlyList<RoutineDefinition>> QueryCatalogAsync(
        string predicate,
        CancellationToken cancellationToken,
        params NpgsqlParameter[] parameters)
    {
        var sql = string.Concat(
            CatalogSelect,
            Environment.NewLine,
            predicate,
            Environment.NewLine,
            "ORDER BY p.proname, pg_catalog.pg_get_function_identity_arguments(p.oid), argument.ordinal_position");

        await using var command = CreateCommand(sql, _options.CommandTimeoutSeconds);
        command.Parameters.AddRange(parameters);
        await EnsureConnectionOpenAsync(command.Connection!, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);

        var routines = new List<RoutineDefinition>();
        RoutineDefinition? current = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            if (current?.Id != id)
            {
                if (routines.Count >= MaximumCatalogItems)
                {
                    break;
                }

                current = new RoutineDefinition(
                    id,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetBoolean(7),
                    reader.GetString(8),
                    reader.GetBoolean(9),
                    reader.GetInt32(10),
                    reader.GetBoolean(11),
                    reader.GetBoolean(12),
                    reader.GetString(13),
                    reader.GetString(14));
                routines.Add(current);
            }

            if (!reader.IsDBNull(15))
            {
                current.Arguments.Add(new RoutineArgumentDefinition(
                    reader.GetInt32(15),
                    reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                    reader.GetString(17),
                    reader.GetString(18),
                    reader.GetString(19),
                    reader.GetString(20),
                    reader.GetString(21),
                    reader.GetString(22)));
            }
        }

        return routines;
    }

    private async Task<RoutineInvocationResponse> ExecuteAsync(
        RoutineDefinition routine,
        IReadOnlyList<RoutineArgumentDefinition> argumentDefinitions,
        List<object?> arguments,
        CancellationToken cancellationToken)
    {
        var placeholders = CreateInvocationPlaceholders(routine, argumentDefinitions, arguments.Count);
        var qualifiedName = GetQualifiedName(routine);
        var invocation = string.Join(", ", placeholders);
        var sql = routine.Type == "PROCEDURE"
            ? $"CALL {qualifiedName}({invocation})"
            : $"SELECT * FROM {qualifiedName}({invocation})";

        await using var command = CreateCommand(sql, _options.RoutineCommandTimeoutSeconds);
        AddArgumentParameters(command, arguments);

        var started = Stopwatch.GetTimestamp();
        await EnsureConnectionOpenAsync(command.Connection!, cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        var boundedRows = await ReadRowsAsync(
            reader,
            _options.MaxRoutineRows,
            _options.MaxTextLength,
            _options.MaxResponseBytes,
            cancellationToken);
        var duration = Stopwatch.GetElapsedTime(started);
        LogInvocationCompleted(_logger, routine.Id, boundedRows.Rows.Count, duration.TotalMilliseconds);
        return new RoutineInvocationResponse(
            routine.Type,
            routine.ReturnsSet,
            boundedRows.Columns,
            boundedRows.Rows,
            boundedRows.Truncated,
            (long)Math.Ceiling(duration.TotalMilliseconds));
    }

    private async Task<RoutineInvocationResponse> ExecuteCursorAsync(
        RoutineDefinition routine,
        IReadOnlyList<RoutineArgumentDefinition> argumentDefinitions,
        List<object?> arguments,
        int maxRows,
        int maxTextLength,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (!routine.ReturnsCursor || routine.Type != "FUNCTION" || routine.ReturnsSet)
        {
            throw new InvalidOperationException("Only a function returning one refcursor can be fetched.");
        }

        var connection = _context.Database.GetDbConnection();
        await EnsureConnectionOpenAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var started = Stopwatch.GetTimestamp();

        var placeholders = CreateInvocationPlaceholders(routine, argumentDefinitions, arguments.Count);
        var callSql = $"SELECT {GetQualifiedName(routine)}({string.Join(", ", placeholders)})";
        string cursorName;
        await using (var callCommand = CreateCommand(callSql, _options.RoutineCommandTimeoutSeconds))
        {
            callCommand.Transaction = transaction;
            AddArgumentParameters(callCommand, arguments);
            cursorName = Convert.ToString(
                await callCommand.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture) ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(cursorName))
        {
            throw new InvalidOperationException("The PostgreSQL function returned an empty refcursor.");
        }

        BoundedRows boundedRows;
        await using (var fetchCommand = CreateCommand(
            $"FETCH FORWARD {maxRows + 1} FROM {QuoteIdentifier(cursorName)}",
            _options.RoutineCommandTimeoutSeconds))
        {
            fetchCommand.Transaction = transaction;
            await using var reader = await fetchCommand.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);
            boundedRows = await ReadRowsAsync(
                reader,
                maxRows,
                maxTextLength,
                maxBytes,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        var duration = Stopwatch.GetElapsedTime(started);
        LogInvocationCompleted(_logger, routine.Id, boundedRows.Rows.Count, duration.TotalMilliseconds);
        return new RoutineInvocationResponse(
            routine.Type,
            true,
            boundedRows.Columns,
            boundedRows.Rows,
            boundedRows.Truncated,
            (long)Math.Ceiling(duration.TotalMilliseconds));
    }

    private static async Task<BoundedRows> ReadRowsAsync(
        DbDataReader reader,
        int maxRows,
        int maxTextLength,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        var estimatedBytes = 0;
        var truncated = false;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (rows.Count >= maxRows)
            {
                truncated = true;
                break;
            }

            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                var (value, valueTruncated) = ReadBoundedValue(reader, index, maxTextLength);
                estimatedBytes += reader.GetName(index).Length * sizeof(char) + EstimateSize(value);
                if (estimatedBytes > maxBytes)
                {
                    truncated = true;
                    break;
                }

                row[reader.GetName(index)] = value;
                truncated |= valueTruncated;
            }

            if (estimatedBytes > maxBytes)
            {
                break;
            }

            rows.Add(row);
        }

        return new(columns, rows, truncated);
    }

    private DatabaseRoutineResponse ToResponse(RoutineDefinition routine)
    {
        var inputArguments = routine.Arguments
            .Where(argument => argument.Mode is "i" or "b")
            .ToArray();
        var requiredCount = Math.Max(0, inputArguments.Length - routine.DefaultArgumentCount);
        var inputPosition = 0;
        var arguments = routine.Arguments.Select(argument =>
        {
            var isInput = argument.Mode is "i" or "b";
            var required = isInput && inputPosition++ < requiredCount;
            return new DatabaseRoutineArgumentResponse(
                argument.Position,
                string.IsNullOrWhiteSpace(argument.Name) ? $"${argument.Position}" : argument.Name,
                GetArgumentMode(argument.Mode),
                argument.DisplayType,
                required);
        }).ToArray();
        var disabledReason = GetInvocationDisabledReason(routine);

        return new DatabaseRoutineResponse(
            routine.Schema,
            routine.Name,
            routine.Type,
            $"{routine.Schema}.{routine.Name}({routine.IdentityArguments})",
            routine.IdentityArgumentTypes,
            routine.ReturnType,
            routine.ReturnsSet,
            routine.ReturnsCursor,
            routine.Volatility,
            routine.SecurityDefiner,
            disabledReason is null,
            disabledReason,
            arguments);
    }

    private string? GetInvocationDisabledReason(RoutineDefinition routine)
    {
        if (!_options.RoutineExecutionEnabled)
        {
            return "Routine execution is disabled by configuration.";
        }

        if (!_options.AllowedRoutineSchemas.Contains(routine.Schema, StringComparer.Ordinal))
        {
            return "The routine schema is not allowlisted for REST execution.";
        }

        if (routine.Type == "PROCEDURE" && !_options.AllowProcedures)
        {
            return "Procedure execution is disabled by configuration.";
        }

        if (routine.SecurityDefiner && !_options.AllowSecurityDefiner)
        {
            return "SECURITY DEFINER routines are blocked by configuration.";
        }

        if (!routine.HasExecutePrivilege)
        {
            return "The API database role does not have EXECUTE privilege.";
        }

        if (routine.IsVariadic)
        {
            return "Variadic routines are not supported by the REST contract.";
        }

        var inputArguments = routine.Arguments
            .Where(argument => argument.Mode is "i" or "b" or "v")
            .ToArray();
        if (routine.Arguments.Count > _options.MaxRoutineArguments)
        {
            return $"The routine exceeds the {_options.MaxRoutineArguments}-argument limit.";
        }

        if (inputArguments.Any(argument => !IsSupportedInputType(argument)))
        {
            return "One or more input types cannot be represented safely by the REST contract.";
        }

        return null;
    }

    private static bool IsSupportedInputType(RoutineArgumentDefinition argument) =>
        argument.TypeKind is "b" or "d" or "e" or "r" or "m" &&
        argument.TypeCategory is not ("A" or "C" or "P");

    private bool TryConvertArgument(
        JsonElement argument,
        RoutineArgumentDefinition definition,
        out object? value)
    {
        if (argument.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            value = null;
            return true;
        }

        if (definition.TypeSchema == "pg_catalog" && definition.TypeName is "json" or "jsonb")
        {
            value = argument.ValueKind == JsonValueKind.String
                ? argument.GetString()
                : argument.GetRawText();
            return value is string json && json.Length <= _options.MaxRoutineArgumentLength;
        }

        if (definition.TypeSchema == "pg_catalog" && definition.TypeName == "bytea")
        {
            if (argument.ValueKind != JsonValueKind.String)
            {
                value = null;
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(argument.GetString()!);
                if (bytes.Length > _options.MaxRoutineArgumentLength)
                {
                    value = null;
                    return false;
                }

                value = string.Concat("\\x", Convert.ToHexString(bytes));
                return true;
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }
        }

        value = argument.ValueKind switch
        {
            JsonValueKind.String => argument.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => argument.GetRawText(),
            _ => null
        };
        return value is string scalar && scalar.Length <= _options.MaxRoutineArgumentLength;
    }

    private static (object? Value, bool Truncated) ReadBoundedValue(
        DbDataReader reader,
        int index,
        int maxTextLength)
    {
        if (reader.IsDBNull(index))
        {
            return (null, false);
        }

        var value = reader.GetValue(index);
        return value switch
        {
            string text when text.Length > maxTextLength =>
                (string.Concat(text.AsSpan(0, maxTextLength), "…"), true),
            string text => (text, false),
            byte[] bytes when bytes.Length > maxTextLength =>
                ($"{Convert.ToBase64String(bytes, 0, maxTextLength)}…", true),
            byte[] bytes => (Convert.ToBase64String(bytes), false),
            Array array when array.Length > 100 =>
                ($"{array.GetType().GetElementType()?.Name ?? "value"}[{array.Length}]", true),
            Array array => (array, false),
            bool or byte or sbyte or short or ushort or int or uint or long or ulong or
                float or double or decimal or DateTime or DateTimeOffset or DateOnly or TimeOnly or
                TimeSpan or Guid => (value, false),
            _ => (Convert.ToString(value, CultureInfo.InvariantCulture), false)
        };
    }

    private static int EstimateSize(object? value) => value switch
    {
        null => 4,
        string text => text.Length * sizeof(char),
        byte[] bytes => bytes.Length,
        Array array => Math.Min(array.Length, 100) * 32,
        _ => 32
    };

    private DbCommand CreateCommand(string sql, int timeoutSeconds)
    {
        var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = timeoutSeconds;
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

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static string GetQualifiedName(RoutineDefinition routine) =>
        $"{QuoteIdentifier(routine.Schema)}.{QuoteIdentifier(routine.Name)}";

    private static List<string> CreateInvocationPlaceholders(
        RoutineDefinition routine,
        IReadOnlyList<RoutineArgumentDefinition> argumentDefinitions,
        int argumentCount)
    {
        var placeholders = new List<string>();
        if (routine.Type == "PROCEDURE")
        {
            var inputIndex = 0;
            foreach (var definition in routine.Arguments)
            {
                if (definition.Mode is "o" or "t")
                {
                    placeholders.Add("NULL");
                    continue;
                }

                placeholders.Add(inputIndex < argumentCount
                    ? CreateArgumentPlaceholder(definition, inputIndex)
                    : "DEFAULT");
                inputIndex++;
            }
        }
        else
        {
            for (var index = 0; index < argumentCount; index++)
            {
                placeholders.Add(CreateArgumentPlaceholder(argumentDefinitions[index], index));
            }
        }

        return placeholders;
    }

    private static void AddArgumentParameters(DbCommand command, List<object?> arguments)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            command.Parameters.Add(new NpgsqlParameter($"argument{index}", NpgsqlDbType.Unknown)
            {
                Value = arguments[index] ?? DBNull.Value
            });
        }
    }

    private static string CreateArgumentPlaceholder(
        RoutineArgumentDefinition definition,
        int inputIndex) => string.Create(
            CultureInfo.InvariantCulture,
            $"CAST(@argument{inputIndex} AS {QuoteIdentifier(definition.TypeSchema)}.{QuoteIdentifier(definition.TypeName)})");

    private static string GetArgumentMode(string mode) => mode switch
    {
        "o" => "OUT",
        "b" => "INOUT",
        "v" => "VARIADIC",
        "t" => "TABLE",
        _ => "IN"
    };

    [LoggerMessage(1, LogLevel.Warning, "Routine {RoutineId} invocation failed with SQLSTATE {SqlState}")]
    private static partial void LogInvocationFailure(
        ILogger logger,
        long routineId,
        string sqlState);

    [LoggerMessage(2, LogLevel.Information,
        "Routine {RoutineId} invocation completed with {RowCount} rows in {DurationMilliseconds} ms")]
    private static partial void LogInvocationCompleted(
        ILogger logger,
        long routineId,
        int rowCount,
        double durationMilliseconds);

    private sealed class RoutineDefinition(
        long id,
        string schema,
        string name,
        string type,
        string identityArguments,
        string identityArgumentTypes,
        string? returnType,
        bool returnsSet,
        string volatility,
        bool securityDefiner,
        int defaultArgumentCount,
        bool isVariadic,
        bool hasExecutePrivilege,
        string returnTypeSchema,
        string returnTypeName)
    {
        public long Id { get; } = id;
        public string Schema { get; } = schema;
        public string Name { get; } = name;
        public string Type { get; } = type;
        public string IdentityArguments { get; } = identityArguments;
        public string IdentityArgumentTypes { get; } = identityArgumentTypes;
        public string? ReturnType { get; } = returnType;
        public bool ReturnsSet { get; } = returnsSet;
        public bool ReturnsCursor { get; } =
            type == "FUNCTION" &&
            !returnsSet &&
            returnTypeSchema == "pg_catalog" &&
            returnTypeName == "refcursor";
        public string Volatility { get; } = volatility;
        public bool SecurityDefiner { get; } = securityDefiner;
        public int DefaultArgumentCount { get; } = defaultArgumentCount;
        public bool IsVariadic { get; } = isVariadic;
        public bool HasExecutePrivilege { get; } = hasExecutePrivilege;
        public List<RoutineArgumentDefinition> Arguments { get; } = [];
    }

    private sealed record RoutineArgumentDefinition(
        int Position,
        string Name,
        string Mode,
        string TypeSchema,
        string TypeName,
        string DisplayType,
        string TypeKind,
        string TypeCategory);

    private enum RoutineResolutionStatus
    {
        Success,
        NotFound,
        Ambiguous
    }

    private sealed record RoutineResolution(
        RoutineResolutionStatus Status,
        RoutineDefinition? Routine = null,
        string? Detail = null);

    private sealed record PreparedInvocation(
        IReadOnlyList<RoutineArgumentDefinition> InputArguments,
        List<object?> Values,
        string? Detail = null);

    private sealed record BoundedRows(
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows,
        bool Truncated);
}
