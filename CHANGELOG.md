# Changelog

## Unreleased

### Added

- .NET 10 API, Vue 3 web console and Docker Compose development stack.
- Revocable short-lived JWT sessions and administrator bootstrap workflow.
- Read-only PostgreSQL metadata and table browser.
- Catalog-driven REST invocation for existing PostgreSQL functions and procedures using named JSON arguments, including independent defaults, `OUT`/`INOUT` values and bounded set results.
- Automatic overload resolution by argument names, defaults, JSON compatibility and numeric preference, with structured ambiguity candidates, detection of default-shadowed PostgreSQL signatures and an optional explicit overload escape hatch.
- Stable schema/name routine URLs with natural scalar/object/array/204 responses; PostgreSQL OIDs are internal only.
- Transactional `refcursor` fetching as JSON and bounded `.xlsx` report generation from validated client workbook settings.
- A local demo schema with callable functions, a cursor-backed Excel report, a mutating procedure and seed data.
- Namespaced Kubernetes provisioner with a disabled/simulated local mode.
- Safe file endpoints, metadata-only request audit and health checks.
- HTTP security tests, frontend session/navigation tests, dependency audits, CodeQL, Trivy, CI and Dependabot.
- Paginated administrative collections, audit retention and correlation response headers.

### Security

- Removed committed credentials, raw token storage, entity overposting and anonymous file routes.
- Removed arbitrary SQL and dynamic DDL while retaining parameter-only stored-routine invocation; removed privileged database bootstrap, hostPath, NodePort and cluster-wide RBAC.
- Replaced arbitrary Excel formulas and unbounded cursor materialization with validated workbook settings and independent report limits.

### Removed

- Template WeatherForecast/SuperHero code and the legacy university SQL/archiving modules.
