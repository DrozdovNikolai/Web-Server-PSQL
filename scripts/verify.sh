#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet tool restore
dotnet restore PostgresControlPlane.sln --locked-mode
dotnet format PostgresControlPlane.sln --no-restore --verify-no-changes
dotnet build PostgresControlPlane.sln --configuration Release --no-restore
dotnet test PostgresControlPlane.sln --configuration Release --no-build
dotnet list PostgresControlPlane.sln package \
  --vulnerable --include-transitive

npm --prefix admin-panel ci
npm --prefix admin-panel run lint
npm --prefix admin-panel test
npm --prefix admin-panel run build
npm --prefix admin-panel audit --audit-level=moderate

docker compose --env-file .env.example config --quiet
