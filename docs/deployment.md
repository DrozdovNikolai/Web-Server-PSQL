# Deployment

## Local Compose

```bash
cp .env.example .env
# Change every credential in .env.
docker compose --env-file .env up --build
```

Compose starts PostgreSQL, the API and the web console. On the first database initialization it also loads safe sample tables, functions and a procedure from `docker/postgres-init`, so the routine REST gateway can be demonstrated immediately. Kubernetes is disabled, so deployment requests are simulated. Data lives in named volumes; stop services with `docker compose down` and remove development data only when intentionally adding `--volumes`.

## Routine exposure

Routine execution is controlled independently from read-only catalog browsing:

```text
DatabaseBrowser__RoutineExecutionEnabled=true
DatabaseBrowser__AllowedRoutineSchemas__0=public
DatabaseBrowser__AllowProcedures=true
DatabaseBrowser__AllowSecurityDefiner=false
DatabaseBrowser__RoutineCommandTimeoutSeconds=10
DatabaseBrowser__MaxRoutineArguments=32
DatabaseBrowser__MaxRoutineArgumentLength=16384
DatabaseBrowser__MaxRoutineRows=200
DatabaseBrowser__RoutineExcelExportEnabled=true
DatabaseBrowser__MaxExcelRows=10000
DatabaseBrowser__MaxExcelColumns=256
DatabaseBrowser__MaxExcelCells=200000
DatabaseBrowser__MaxExcelFileSizeBytes=10485760
```

Excel export is accepted only for a non-set-returning function whose catalog
return type is exactly `pg_catalog.refcursor`. Keep the export switch disabled
when reports are not needed, and size it below the API container's available
memory. The generator holds one bounded workbook in memory before returning it.

The API connection role needs catalog access and `EXECUTE` only for routines intended to be exposed. PostgreSQL normally grants routine execution to `PUBLIC`, so a production database should first revoke that broad default and grant narrowly:

```sql
REVOKE EXECUTE ON FUNCTION public.calculate_order_total(integer, numeric) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.calculate_order_total(integer, numeric) TO control_plane_api;

REVOKE EXECUTE ON PROCEDURE public.record_demo_event(text) FROM PUBLIC;
GRANT EXECUTE ON PROCEDURE public.record_demo_event(text) TO control_plane_api;
```

Do not enable a schema merely because it is named `public`: review the routine bodies and their transitive privileges. `SECURITY DEFINER` remains blocked unless the operator explicitly accepts that privilege boundary.

## Kubernetes

The manifests in `k8s/` are production-oriented examples, not a turnkey public deployment.

1. Build and push an immutable API image.
2. Replace both example image references in `k8s/deployment.yaml` and `k8s/configmap.yaml`.
3. Copy `k8s/secret.example.yaml` to ignored `k8s/secret.yaml` and provide a least-privileged PostgreSQL connection, a random JWT key and bootstrap credentials.
4. Narrow the NetworkPolicy egress destinations to the database and Kubernetes API used by the cluster.
5. Apply the base manifests with `./deploy.ps1` or your GitOps system.
6. Configure TLS and the hostname in `k8s/ingress.example.yaml` before applying it separately.

Dynamic workload ingress is disabled unless `Kubernetes__PublicBaseUrl` is set. When it is enabled, the URL must use HTTPS and `Kubernetes__IngressTlsSecretName` must identify an existing certificate Secret in the same namespace.

The control-plane ServiceAccount has a namespace-scoped Role because optional provisioning creates workloads in the same namespace. It has no list/watch access, and dynamically created workload pods do not mount service-account tokens. ResourceQuota and LimitRange cap the demo namespace even when a provisioning workflow is repeated.

## Hosting recommendation

For a public portfolio demo, run the frontend as a static container, the API as a container service and PostgreSQL as a managed database. Keep `Kubernetes__Enabled=false`; demonstrate the Kubernetes adapter through diagrams and local cluster tests unless the demo has a dedicated sandbox cluster.

Set secrets in the hosting platform, enable HTTPS, configure backups and run the readiness endpoint as the deployment health check. Swagger should remain disabled in Production unless protected separately.

TLS is terminated by the hosting proxy or Kubernetes ingress. Forward only HTTPS traffic to public clients; the API container itself listens on internal HTTP port 8080.
Configure the proxy address through `Proxy__KnownProxies__0`; forwarded scheme and client IP headers from every other address are ignored.

## Database migration and backup

Restore the repository-local EF tool and inspect the idempotent migration script before a release:

```bash
dotnet tool restore
dotnet ef migrations script --idempotent \
  --project src/PostgresControlPlane.Api/PostgresControlPlane.Api.csproj \
  --startup-project src/PostgresControlPlane.Api/PostgresControlPlane.Api.csproj \
  --output migrations.sql
```

For PostgreSQL, take a provider snapshot or `pg_dump --format=custom` before applying a migration. Restore into a separate database with `pg_restore`, verify `/health/ready`, then switch traffic. Do not use an untested backup or an application pod filesystem as the only copy.
