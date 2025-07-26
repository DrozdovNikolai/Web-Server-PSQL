# ADR 0002: Expose stored routines through a stable REST gateway

Status: accepted.

## Context

The defining feature of the original diploma was invoking PostgreSQL stored functions and procedures through HTTP. Removing every execution path made the rebuild safer but also removed the product's reason to exist. The first replacement used PostgreSQL OIDs in public URLs. That selected overloads safely, but OIDs change after routine recreation or database restore and therefore make a poor frontend contract.

The original project also treated functions returning `refcursor` as report sources and could render those results as Excel workbooks. Both outcomes are product capabilities worth preserving, while arbitrary function names, SQL fragments and formulas from the legacy implementation are not.

## Decision

The API exposes three operations:

1. catalog discovery returns exact function/procedure signatures, canonical identity argument types, modes, defaults, volatility, result kind and an invocation decision;
2. invocation uses `POST /routines/{schema}/{name}/invoke` with `identityArguments` and an ordered JSON argument array;
3. a non-set-returning function whose catalog return type is exactly `pg_catalog.refcursor` can also use `/export/xlsx` with validated workbook settings.

`identityArguments` is the canonical input-type list returned by `pg_catalog.oidvectortypes(proargtypes)`. It is optional when a schema/name pair is unique and required to select an overload. The catalog lookup is parameterized. After resolution, the server quotes the schema, routine and type identifiers from the catalog row and binds every argument value as an Npgsql parameter. The PostgreSQL OID is retained only for internal logging and privilege checks.

Functions use a generated `SELECT`; procedures use a generated `CALL`. A `refcursor` function is called and fetched within one transaction because the cursor ceases to exist at transaction end. JSON invocation returns only the routine's natural value: a scalar, object, array or `204 No Content`. Timing and truncation signals use logs or HTTP headers rather than a metadata envelope.

Excel generation uses ClosedXML. The contract supports the worksheet/file name, report origin, auto-fit, freeze panes, row/column sizing, bounded cell/range overrides and safe styles. It does not accept arbitrary formulas. Cursor source rows, columns, text, source bytes and final workbook bytes have independent limits.

Invocation requires the separate `Database.Execute` policy. Only configured schemas are eligible. The API also checks the connection role's `EXECUTE` privilege and blocks `SECURITY DEFINER`, variadic, array, composite and pseudo-type inputs by default.

## Consequences

- Frontends use a readable contract that survives database restore and routine recreation when the signature is unchanged.
- Overloads remain deterministic without exposing an unstable OID.
- The diploma's cursor-to-JSON and cursor-to-Excel report flow is preserved without accepting SQL or formulas.
- Procedures and volatile functions can intentionally mutate state; configuration is not a substitute for reviewing routines and granting a least-privileged API role.
- Some PostgreSQL inputs remain excluded until they have an unambiguous JSON representation and integration tests.
