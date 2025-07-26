# Contributing

## Setup

Install .NET SDK 10.0.302+, Node.js 22.20+ and Docker. Copy `.env.example` to `.env`, change the local credentials, then run:

```bash
docker compose --env-file .env up --build
```

## Before opening a pull request

```bash
./scripts/verify.sh
# Windows PowerShell: ./scripts/verify.ps1
```

Warnings are treated as errors. Keep API contracts separate from EF entities, pass cancellation tokens to I/O, return `ProblemDetails` instead of infrastructure exceptions and never add an endpoint that executes arbitrary client-provided SQL. Stored-routine changes must resolve identity and types from `pg_catalog`, bind values as parameters, preserve response limits and include a real PostgreSQL Testcontainer scenario.

Do not commit `.env`, local appsettings, certificates, Kubernetes Secrets, uploads or generated frontend assets. Add or update security tests when changing authentication, authorization, SQL, files or deployment provisioning.
