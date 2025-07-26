using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;
using Testcontainers.PostgreSql;
using Xunit;

namespace PostgresControlPlane.Tests;

public sealed class RoutineInvocationApiTests
{
    private static readonly int[] BlockedArrayValues = [1, 2];

    [Fact]
    public async Task StableRoutineGatewayInvokesFunctionsProceduresCursorsAndExcelExports()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17.7-alpine")
            .WithDatabase("routine_tests")
            .WithUsername("routine_tests")
            .WithPassword("routine-tests-password")
            .Build();
        await postgres.StartAsync();

        await using var factory = new PostgreSqlRoutineApiFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();
        const string password = "integration-password";
        await factory.SeedUserAsync("routine-admin", password, ApplicationRoles.SuperAdministrator);
        await factory.ExecuteSqlAsync(
            """
            CREATE TABLE public.invocation_log(message text NOT NULL);
            CREATE TABLE public.report_orders(
                id integer PRIMARY KEY,
                customer text NOT NULL,
                total numeric(12, 2) NOT NULL);
            INSERT INTO public.report_orders VALUES
                (1, 'Ada', 120.00),
                (2, 'Grace', 245.50),
                (3, 'Edsger', 89.90);

            CREATE FUNCTION public.add_tax(base numeric, tax numeric DEFAULT 0)
            RETURNS numeric
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT base + (base * tax) $$;

            CREATE FUNCTION public.overloaded(value integer)
            RETURNS integer
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT value * 2 $$;

            CREATE FUNCTION public.overloaded(value bigint)
            RETURNS bigint
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT value * 3 $$;

            CREATE FUNCTION public.overloaded(value text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT 'text:' || value $$;

            CREATE FUNCTION public.route_by_name(quantity integer)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT 'quantity:' || quantity::text $$;

            CREATE FUNCTION public.route_by_name(source text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT 'source:' || source $$;

            CREATE FUNCTION public.price_parts(
                base numeric,
                tax numeric DEFAULT 0.2,
                fee numeric DEFAULT 0)
            RETURNS numeric
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT base * (1 + tax) + fee $$;

            CREATE FUNCTION public.default_shadow(value integer)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT 'short:' || value::text $$;

            CREATE FUNCTION public.default_shadow(value integer, factor integer DEFAULT 2)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT 'long:' || (value * factor)::text $$;

            CREATE FUNCTION public.unnamed(integer)
            RETURNS integer
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT $1 * 3 $$;

            CREATE FUNCTION public.describe_payload(enabled boolean, id uuid, payload jsonb)
            RETURNS jsonb
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT jsonb_build_object('enabled', enabled, 'id', id, 'payload', payload) $$;

            CREATE FUNCTION public.blocked_array(items integer[])
            RETURNS integer
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT cardinality(items) $$;

            CREATE PROCEDURE public.record_invocation(message text)
            LANGUAGE sql
            AS $$ INSERT INTO public.invocation_log(message) VALUES (message) $$;

            CREATE PROCEDURE public.compute_pair(IN value integer, OUT doubled integer)
            LANGUAGE plpgsql
            AS $$ BEGIN doubled := value * 2; END $$;

            CREATE FUNCTION public.generate_numbers(maximum integer)
            RETURNS SETOF integer
            LANGUAGE sql
            IMMUTABLE
            AS $$ SELECT generate_series(1, maximum) $$;

            CREATE FUNCTION public.open_order_report(minimum numeric DEFAULT 0)
            RETURNS refcursor
            LANGUAGE plpgsql
            STABLE
            AS $$
            DECLARE result_cursor refcursor := 'order_report_cursor';
            BEGIN
                OPEN result_cursor FOR
                    SELECT id AS order_id, customer, total
                    FROM public.report_orders
                    WHERE total >= minimum
                    ORDER BY id;
                RETURN result_cursor;
            END;
            $$;

            CREATE FUNCTION public.open_large_report()
            RETURNS refcursor
            LANGUAGE plpgsql
            STABLE
            AS $$
            DECLARE result_cursor refcursor := 'large_report_cursor';
            BEGIN
                OPEN result_cursor FOR SELECT generate_series(1, 101) AS value;
                RETURN result_cursor;
            END;
            $$;

            CREATE FUNCTION public.slow_function()
            RETURNS void
            LANGUAGE sql
            AS $$ SELECT pg_sleep(3) $$;

            CREATE FUNCTION public.elevated_echo(value text)
            RETURNS text
            LANGUAGE sql
            SECURITY DEFINER
            AS $$ SELECT value $$;

            CREATE SCHEMA private_api;
            CREATE FUNCTION private_api.hidden_echo(value text)
            RETURNS text
            LANGUAGE sql
            AS $$ SELECT value $$;
            """);
        await AuthenticateAsync(client, "routine-admin", password);

        var routines = await client.GetFromJsonAsync<IReadOnlyList<DatabaseRoutineResponse>>(
            "/api/database/routines?schema=public");
        Assert.NotNull(routines);

        var addTax = Assert.Single(routines, routine => routine.Name == "add_tax");
        Assert.True(addTax.CanInvoke);
        Assert.False(addTax.ReturnsCursor);
        Assert.Equal("FUNCTION", addTax.Type);
        Assert.Equal("public.add_tax(base numeric, tax numeric)", addTax.Signature);
        Assert.Equal("numeric, numeric", addTax.Overload);
        Assert.Equal(2, addTax.Arguments.Count);
        Assert.True(addTax.Arguments[0].Required);
        Assert.False(addTax.Arguments[1].Required);

        var addTaxResponse = await InvokeAsync(client, addTax, 100, 0.2);
        Assert.Equal(HttpStatusCode.OK, addTaxResponse.StatusCode);
        using (var result = JsonDocument.Parse(await addTaxResponse.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(120m, result.RootElement.GetDecimal());
        }

        var defaultArgumentResponse = await InvokeAsync(client, addTax, 25);
        Assert.Equal(HttpStatusCode.OK, defaultArgumentResponse.StatusCode);

        var overloads = routines.Where(routine => routine.Name == "overloaded").ToArray();
        Assert.Equal(3, overloads.Length);
        var integerOverload = Assert.Single(overloads, routine => routine.Overload == "integer");
        var bigintOverload = Assert.Single(overloads, routine => routine.Overload == "bigint");
        var textOverload = Assert.Single(overloads, routine => routine.Overload == "text");

        var inferredIntegerResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/overloaded/invoke",
            new { arguments = new { value = 21 } });
        Assert.Equal(HttpStatusCode.OK, inferredIntegerResponse.StatusCode);
        Assert.Equal(42, await ReadScalarAsync<int>(inferredIntegerResponse));

        var inferredBigintResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/overloaded/invoke",
            new { arguments = new { value = (long)int.MaxValue + 1 } });
        Assert.Equal(HttpStatusCode.OK, inferredBigintResponse.StatusCode);
        Assert.Equal(6_442_450_944L, await ReadScalarAsync<long>(inferredBigintResponse));

        var inferredTextResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/overloaded/invoke",
            new { arguments = new { value = "hello" } });
        Assert.Equal(HttpStatusCode.OK, inferredTextResponse.StatusCode);
        Assert.Equal("text:hello", await ReadScalarAsync<string>(inferredTextResponse));

        var ambiguousResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/overloaded/invoke",
            new { arguments = new Dictionary<string, object?> { ["value"] = null } });
        Assert.Equal(HttpStatusCode.Conflict, ambiguousResponse.StatusCode);
        using (var problem = JsonDocument.Parse(await ambiguousResponse.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(3, problem.RootElement.GetProperty("candidates").GetArrayLength());
        }

        Assert.Equal(HttpStatusCode.OK, (await InvokeAsync(client, integerOverload, 21)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await InvokeAsync(client, bigintOverload, "21")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await InvokeAsync(client, textOverload, "hello")).StatusCode);

        var routedByName = await client.PostAsJsonAsync(
            "/api/database/routines/public/route_by_name/invoke",
            new { arguments = new { quantity = 7 } });
        Assert.Equal("quantity:7", await ReadScalarAsync<string>(routedByName));

        var defaultGap = await client.PostAsJsonAsync(
            "/api/database/routines/public/price_parts/invoke",
            new { arguments = new { @base = 100, fee = 5 } });
        Assert.Equal(HttpStatusCode.OK, defaultGap.StatusCode);
        Assert.Equal(125m, await ReadScalarAsync<decimal>(defaultGap));

        var defaultShadow = await client.PostAsJsonAsync(
            "/api/database/routines/public/default_shadow/invoke",
            new { arguments = new { value = 3 } });
        Assert.Equal(HttpStatusCode.Conflict, defaultShadow.StatusCode);
        var explicitShadow = await client.PostAsJsonAsync(
            "/api/database/routines/public/default_shadow/invoke",
            new { overload = "integer", arguments = new { value = 3 } });
        Assert.Equal(HttpStatusCode.Conflict, explicitShadow.StatusCode);
        var resolvedShadow = await client.PostAsJsonAsync(
            "/api/database/routines/public/default_shadow/invoke",
            new { arguments = new { value = 3, factor = 4 } });
        Assert.Equal("long:12", await ReadScalarAsync<string>(resolvedShadow));

        var unnamed = Assert.Single(routines, routine => routine.Name == "unnamed");
        Assert.Equal("$1", Assert.Single(unnamed.Arguments).Name);
        var unnamedResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/unnamed/invoke",
            new { arguments = new Dictionary<string, object?> { ["$1"] = 4 } });
        Assert.Equal(12, await ReadScalarAsync<int>(unnamedResponse));

        var typedJsonResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/describe_payload/invoke",
            new
            {
                arguments = new
                {
                    enabled = true,
                    id = "4f9f2b70-ec96-44af-996f-12eb99c13a67",
                    payload = new { source = "REST", count = 2 }
                }
            });
        Assert.Equal(HttpStatusCode.OK, typedJsonResponse.StatusCode);
        using (var result = JsonDocument.Parse(await typedJsonResponse.Content.ReadAsStreamAsync()))
        {
            Assert.True(result.RootElement.GetProperty("enabled").GetBoolean());
            Assert.Equal("REST", result.RootElement.GetProperty("payload").GetProperty("source").GetString());
        }

        var procedure = Assert.Single(routines, routine => routine.Name == "record_invocation");
        var procedureResponse = await InvokeAsync(client, procedure, "called-through-rest");
        Assert.Equal(HttpStatusCode.NoContent, procedureResponse.StatusCode);
        Assert.Equal(1L, await factory.ExecuteScalarAsync<long>("SELECT count(*) FROM public.invocation_log"));

        var outputProcedure = Assert.Single(routines, routine => routine.Name == "compute_pair");
        var outputResponse = await InvokeAsync(client, outputProcedure, 6);
        Assert.Equal(HttpStatusCode.OK, outputResponse.StatusCode);
        using (var result = JsonDocument.Parse(await outputResponse.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(12, result.RootElement.GetProperty("doubled").GetInt32());
        }

        var setFunction = Assert.Single(routines, routine => routine.Name == "generate_numbers");
        var setResponse = await InvokeAsync(client, setFunction, 100);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        Assert.True(setResponse.Headers.TryGetValues("X-Result-Truncated", out var truncatedValues));
        Assert.Equal("true", Assert.Single(truncatedValues));
        using (var result = JsonDocument.Parse(await setResponse.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(25, result.RootElement.GetArrayLength());
            Assert.Equal(1, result.RootElement[0].GetInt32());
        }

        var cursorFunction = Assert.Single(routines, routine => routine.Name == "open_order_report");
        Assert.True(cursorFunction.ReturnsCursor);
        var cursorResponse = await InvokeAsync(client, cursorFunction, 100);
        Assert.Equal(HttpStatusCode.OK, cursorResponse.StatusCode);
        using (var result = JsonDocument.Parse(await cursorResponse.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(2, result.RootElement.GetArrayLength());
            Assert.Equal("Ada", result.RootElement[0].GetProperty("customer").GetString());
            Assert.Equal("Grace", result.RootElement[1].GetProperty("customer").GetString());
        }

        var excelResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/open_order_report/export/xlsx",
            new
            {
                arguments = new { minimum = 100 },
                excel = new
                {
                    fileName = "orders-report.xlsx",
                    sheetName = "Orders",
                    reportStartPosition = new { row = 2, column = 2 },
                    autoFitColumns = false,
                    columnWidths = new Dictionary<int, double> { [0] = 18 },
                    rowHeights = new Dictionary<int, double> { [0] = 24 },
                    freezePane = new { rows = 2, columns = 1 },
                    ranges = new[]
                    {
                        new
                        {
                            top = 0,
                            left = 0,
                            bottom = 0,
                            right = 2,
                            merge = false,
                            style = new { bold = true, border = true, fillColor = "DFF3E8" }
                        }
                    },
                    cells = new[]
                    {
                        new { row = 0, column = 3, value = "=SUM(1,1)", valueType = "string" }
                    }
                }
            });
        Assert.Equal(HttpStatusCode.OK, excelResponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            excelResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("orders-report.xlsx", excelResponse.Content.Headers.ContentDisposition?.FileNameStar);

        await using (var excelStream = await excelResponse.Content.ReadAsStreamAsync())
        using (var workbook = new XLWorkbook(excelStream))
        {
            var worksheet = workbook.Worksheet("Orders");
            Assert.Equal("order_id", worksheet.Cell(2, 2).GetString());
            Assert.Equal("Ada", worksheet.Cell(3, 3).GetString());
            Assert.Equal(245.5, worksheet.Cell(4, 4).GetDouble(), precision: 2);
            Assert.Equal("=SUM(1,1)", worksheet.Cell(2, 5).GetString());
            Assert.False(worksheet.Cell(2, 5).HasFormula);
            Assert.True(worksheet.Cell(2, 5).Style.IncludeQuotePrefix);
            Assert.Equal(18, worksheet.Column(2).Width, precision: 2);
            Assert.Equal(24, worksheet.Row(2).Height, precision: 2);
        }

        var largeCursor = Assert.Single(routines, routine => routine.Name == "open_large_report");
        var largeExport = await client.PostAsJsonAsync(
            "/api/database/routines/public/open_large_report/export/xlsx",
            new
            {
                arguments = new { },
                excel = new { }
            });
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, largeExport.StatusCode);

        var nonCursorExport = await client.PostAsJsonAsync(
            "/api/database/routines/public/add_tax/export/xlsx",
            new
            {
                arguments = new { @base = 100, tax = 0.2 },
                excel = new { }
            });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nonCursorExport.StatusCode);

        var slowFunction = Assert.Single(routines, routine => routine.Name == "slow_function");
        Assert.Equal(HttpStatusCode.GatewayTimeout, (await InvokeAsync(client, slowFunction)).StatusCode);

        var securityDefiner = Assert.Single(routines, routine => routine.Name == "elevated_echo");
        Assert.False(securityDefiner.CanInvoke);
        Assert.Contains("SECURITY DEFINER", securityDefiner.InvocationDisabledReason, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await InvokeAsync(client, securityDefiner, "blocked")).StatusCode);

        var blockedArray = Assert.Single(routines, routine => routine.Name == "blocked_array");
        Assert.False(blockedArray.CanInvoke);
        var blockedArrayResponse = await client.PostAsJsonAsync(
            "/api/database/routines/public/blocked_array/invoke",
            new { arguments = new { items = BlockedArrayValues } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blockedArrayResponse.StatusCode);

        var privateRoutines = await client.GetFromJsonAsync<IReadOnlyList<DatabaseRoutineResponse>>(
            "/api/database/routines?schema=private_api");
        var hidden = Assert.Single(privateRoutines!);
        Assert.False(hidden.CanInvoke);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await InvokeAsync(client, hidden, "blocked")).StatusCode);

        var injectionResponse = await InvokeAsync(
            client,
            addTax,
            "1); DROP TABLE public.invocation_log; --",
            0);
        Assert.Equal(HttpStatusCode.BadRequest, injectionResponse.StatusCode);
        Assert.Equal(1L, await factory.ExecuteScalarAsync<long>("SELECT count(*) FROM public.invocation_log"));

        Assert.Equal(HttpStatusCode.BadRequest, (await InvokeAsync(client, addTax)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await InvokeAsync(
            client,
            textOverload,
            new string('x', 129))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
            "/api/database/routines/public/add_tax/invoke",
            new { overload = "text", arguments = new { @base = "x" } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            "/api/database/routines/public/add_tax/invoke",
            new { arguments = new object[] { 100, 0.2 } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(
            "/api/database/routines/public/add_tax/invoke",
            new StringContent(
                """{"arguments":{"base":100,"base":200}}""",
                Encoding.UTF8,
                "application/json"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
            "/api/database/routines/4294967296/invoke",
            new { arguments = Array.Empty<object>() })).StatusCode);
    }

    private static Task<HttpResponseMessage> InvokeAsync(
        HttpClient client,
        DatabaseRoutineResponse routine,
        params object?[] arguments)
    {
        var inputArguments = routine.Arguments
            .Where(argument => argument.Mode is "IN" or "INOUT")
            .ToArray();
        var namedArguments = arguments
            .Select((value, index) => new { inputArguments[index].Name, Value = value })
            .ToDictionary(argument => argument.Name, argument => argument.Value);
        return client.PostAsJsonAsync(
            $"/api/database/routines/{Uri.EscapeDataString(routine.Schema)}/{Uri.EscapeDataString(routine.Name)}/invoke",
            new { overload = routine.Overload, arguments = namedArguments });
    }

    private static async Task<T> ReadScalarAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<T>();
        return value ?? throw new InvalidOperationException("The routine returned no JSON value.");
    }

    private static async Task AuthenticateAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
    }
}
