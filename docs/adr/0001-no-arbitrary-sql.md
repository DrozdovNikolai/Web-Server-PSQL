# ADR 0001: Do not expose arbitrary SQL

Status: accepted.

## Context

The diploma version accepted SQL fragments, function bodies and DDL through public controller actions. Identifier interpolation, role switching and execution through a highly privileged connection made reliable isolation impractical.

## Decision

The public API does not accept SQL text, function bodies or DDL. Its browser uses server-generated catalog and table queries. Schema and table identifiers use a strict grammar and quoting; all values are parameters. Results are paginated and commands have a short timeout.

Pre-existing stored functions and procedures are exposed through the separate routine gateway described by ADR 0002. Parameterized schema/name catalog lookup plus named JSON arguments resolve their identity and types from `pg_catalog`; a canonical overload is optional for unresolved ties, and the HTTP client never handles the internal OID. Creating or editing routines, DDL and general SQL execution remain outside the public product scope.

## Consequences

The project preserves the original diploma's routine-over-REST idea without becoming a database command shell. Clients cannot smuggle statements through identifiers, type declarations or argument values.
