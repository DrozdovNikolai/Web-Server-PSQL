# ADR 0002: Expose stored routines through a stable REST gateway

Status: accepted.

## Context

The defining feature of the original diploma was invoking PostgreSQL stored functions and procedures through HTTP. Removing every execution path made the rebuild safer but also removed the product's reason to exist. The first replacement used PostgreSQL OIDs in public URLs. That selected overloads safely, but OIDs change after routine recreation or database restore and therefore make a poor frontend contract.

The original project also treated functions returning `refcursor` as report sources and could render those results as Excel workbooks. Both outcomes are product capabilities worth preserving, while arbitrary function names, SQL fragments and formulas from the legacy implementation are not.

## Decision

The API exposes three operations:

1. catalog discovery returns exact function/procedure signatures, a canonical overload value, argument names/types, modes, defaults, volatility, result kind and an invocation decision;
2. invocation uses `POST /routines/{schema}/{name}/invoke` with a named JSON argument object;
3. a non-set-returning function whose catalog return type is exactly `pg_catalog.refcursor` can also use `/export/xlsx` with validated workbook settings.

The server normally selects an overload without client type metadata. It filters candidates by supplied argument names, required/default parameters and JSON-kind compatibility, then applies deterministic numeric preferences. If equally good candidates remain—most notably for `null`—the API returns `409` with canonical overload candidates. A caller may retry with the optional `overload` value returned by the catalog; it is the type-only list from `pg_catalog.oidvectortypes(proargtypes)`.

PostgreSQL itself cannot disambiguate `f(integer)` from `f(integer, integer DEFAULT 1)` when only the first argument is supplied, even with explicit casts. The resolver detects this identical callable shape before execution and returns `409`; the caller must supply the additional argument or the database author must remove the shadowing definition. An explicit overload is deliberately not presented as a false solution to that database-level ambiguity.

The catalog lookup is parameterized. Request keys are used only to match catalog metadata: the schema, routine, parameter and cast identifiers emitted into SQL always come from the resolved catalog row and are quoted. Every value is bound as an Npgsql parameter. Duplicate JSON keys are rejected and PostgreSQL OIDs are retained only for internal logging and privilege checks.

Functions use a generated `SELECT`; procedures use a generated `CALL`. A `refcursor` function is called and fetched within one transaction because the cursor ceases to exist at transaction end. JSON invocation returns only the routine's natural value: a scalar, object, array or `204 No Content`. Timing and truncation signals use logs or HTTP headers rather than a metadata envelope.

Excel generation uses ClosedXML. The contract supports the worksheet/file name, report origin, auto-fit, freeze panes, row/column sizing, bounded cell/range overrides and safe styles. It does not accept arbitrary formulas. Cursor source rows, columns, text, source bytes and final workbook bytes have independent limits.

Invocation requires the separate `Database.Execute` policy. Only configured schemas are eligible. The API also checks the connection role's `EXECUTE` privilege and blocks `SECURITY DEFINER`, variadic, array, composite and pseudo-type inputs by default.

## Consequences

- Ordinary frontends send their business argument names and JSON values without discovering PostgreSQL types first.
- Overloads resolve automatically in deterministic cases; genuinely ambiguous calls fail explicitly instead of silently choosing a possibly wrong routine.
- Generic consoles can pin the exact selected overload without exposing an unstable OID.
- The diploma's cursor-to-JSON and cursor-to-Excel report flow is preserved without accepting SQL or formulas.
- Procedures and volatile functions can intentionally mutate state; configuration is not a substitute for reviewing routines and granting a least-privileged API role.
- Some PostgreSQL inputs remain excluded until they have an unambiguous JSON representation and integration tests.
