# Security policy

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use GitHub private vulnerability reporting for this repository, or contact the maintainer privately through the email listed on their GitHub profile.

Include the affected route or component, reproduction steps, impact and any suggested mitigation. Routine-gateway reports should identify the catalog signature but must redact argument values that contain secrets. Do not access data that is not yours, invoke destructive routines against a hosted instance or attempt to escape the configured Kubernetes namespace.

## Supported version

Only the latest commit on the default branch is supported while the project is pre-1.0.

## Secret handling

Tracked configuration contains placeholders and localhost-only example values. Production secrets must come from the hosting platform or Kubernetes Secret. Before publishing a fork, rotate every JWT key, database password, administrator password and Kubernetes credential that may have existed in its Git history.

The CI pipeline scans Git history for secrets, but a clean scan is not evidence that an already exposed credential remains safe.
