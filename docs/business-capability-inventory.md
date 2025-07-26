# Business capability inventory

This document separates the useful ideas of the original diploma project from
their legacy implementation. It is based on the Git history and the controllers
that existed before the 2026 rewrite. A refactor is not complete when the code is
cleaner but a capability silently disappears; this list is the regression
checklist for future changes.

## Capability map

| Original capability | Business purpose | Current status | Direction |
|---|---|---|---|
| Invoke PostgreSQL functions and procedures over HTTP | Turn database routines into an API without writing a controller per routine | Preserved and redesigned | Keep as the core product. Resolve a stable schema/name/signature on the server; PostgreSQL OIDs remain internal. |
| Invoke functions returning `refcursor` | Return report-shaped database results through REST | Restored | Fetch the cursor in the same transaction and return its rows, not the temporary cursor name. |
| Export a `refcursor` result to Excel | Let the database define a report query while the client controls workbook presentation | Restored | Generate bounded `.xlsx` files from validated JSON settings. |
| Generate table CRUD routines and expose them as REST | Rapidly publish database-backed CRUD resources | Not restored | Candidate feature. Rebuild from templates and catalog metadata; never concatenate request SQL. |
| Inspect functions, procedures, tables and triggers | Discover database capabilities | Preserved as read-only catalog | Keep. Add definitions only after safe redaction and authorization are designed. |
| Create/update/delete functions, procedures, tables and triggers | Administer a PostgreSQL schema remotely | Not restored | Optional future database-workbench module. Do not restore endpoints that accept arbitrary SQL by default. |
| Per-user assignments for tables/functions/procedures and PostgreSQL roles | Restrict which database objects each user may use | Partially preserved | Application roles and PostgreSQL `EXECUTE` checks exist; object-level grants need an explicit model before multi-user hosting. |
| Upload/download files through PostgreSQL functions | Allow routines to validate or store binary documents | Not restored | Candidate feature using bounded `bytea`/large-object contracts and authenticated streaming. |
| General file upload/list/download | Exchange generated reports and documents | Preserved with authenticated bounded storage | Keep the new implementation; never restore anonymous access, absolute host paths or `chmod 777`. |
| Soft delete, scheduled archive, archive search and restore | Retain deleted business data for a controlled period | Not restored | Candidate module. It needs per-table opt-in, retention settings, authorization and integration tests. |
| User registration, profile update, roles, active sessions and revocation | Identity and access management | Preserved and hardened | Keep. |
| Create, update, restart and delete deployments | Provision isolated API workloads | Preserved through a namespaced deployment adapter | Keep optional; it is supporting infrastructure rather than the diploma's central idea. |

## Legacy mechanisms that are intentionally not preserved

- arbitrary SQL submitted in request bodies;
- identifiers concatenated directly into SQL;
- trying every role from a JWT until a query succeeds;
- anonymous file upload, listing and download;
- host-specific paths and world-writable filesystem permissions;
- returning PostgreSQL exception details to clients;
- accepting arbitrary Excel formulas from request JSON.

These are implementation defects, not product capabilities. Their safe
replacements should preserve the user-visible outcome without reproducing the
old trust model.

## Core regression checklist

Before a release, verify all of the following against a real PostgreSQL instance:

1. Scalar, composite, set-returning and overloaded functions can be invoked.
2. Procedures with side effects and `OUT` parameters can be invoked.
3. A `refcursor` function returns fetched rows through JSON.
4. The same `refcursor` result can be downloaded as a valid `.xlsx` workbook.
5. Trailing PostgreSQL defaults and explicit `NULL` arguments work.
6. Disallowed schemas, missing privileges and `SECURITY DEFINER` routines are blocked.
7. Routine identifiers and values cannot alter the generated SQL structure.
8. Response, cursor, workbook, file and execution-time limits are enforced.
