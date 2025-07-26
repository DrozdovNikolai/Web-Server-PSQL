# ADR 0001: Do not expose arbitrary SQL

Status: accepted.

## Context

The diploma version accepted SQL fragments, function bodies and DDL through public controller actions. Identifier interpolation, role switching and execution through a highly privileged connection made reliable isolation impractical.

## Decision

The public API does not accept SQL text, function bodies or DDL. Its browser uses server-generated catalog and table queries. Schema and table identifiers use a strict grammar and quoting; all values are parameters. Results are paginated and commands have a short timeout.

Pre-existing stored functions and procedures are exposed through the separate routine gateway described by ADR 0002. A parameterized schema/name/signature lookup resolves their identity and types from `pg_catalog`; the HTTP client supplies values only and never handles the internal OID. Creating or editing routines, DDL and general SQL execution remain outside the public product scope.

## Consequences

The project preserves the original diploma's routine-over-REST idea without becoming a database command shell. Clients cannot smuggle statements through identifiers, type declarations or argument values.
