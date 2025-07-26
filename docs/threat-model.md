# Threat model

## Assets

- Control-plane user accounts and role assignments.
- PostgreSQL data reachable by the configured database account.
- Kubernetes namespace resources and workload Secrets.
- JWT signing key and active session identifiers.
- Uploaded files and audit metadata.

## Trust boundaries

1. Browser to reverse proxy/API.
2. API to PostgreSQL.
3. API pod to the Kubernetes API.
4. API process to its writable storage volume.

## Primary threats and controls

| Threat | Control |
|---|---|
| Credential stuffing | generic login errors, rate limit, BCrypt work factor 12 |
| Stolen JWT | 15-minute lifetime, issuer/audience checks, server-side `jti` revocation, no long-lived refresh token |
| IDOR / privilege escalation | self-service profile route plus separate administrator policies |
| SQL injection | no SQL text in the contract; schema/name/optional-overload catalog filters and argument values are parameters; request argument names only match metadata, while all executable identifiers, named-notation tokens and casts come from the resolved `pg_catalog` row and are quoted |
| Overload confusion | deterministic name/default/JSON-kind matching, conservative ties, structured `409` candidates, optional exact overload and duplicate JSON-key rejection |
| Unexpected routine privilege | schema allowlist, separate execution policy, PostgreSQL `EXECUTE` grants and default block for `SECURITY DEFINER` routines |
| Data exhaustion | page-size, command timeout, cancellation and response limits |
| Cursor/Excel exhaustion | cursor fetch remains transactional and has independent row/column/cell/source/file limits; workbook settings and worksheet bounds are validated |
| Spreadsheet formula injection | database text is written as text with quote-prefix protection; the JSON workbook contract does not accept formulas |
| Secret disclosure | DTO allowlists, excluded control-plane schema, no raw token persistence, Kubernetes Secrets, body-free audit |
| File traversal or polyglot upload | generated IDs, canonical containment, size/type/signature allowlists and Office package validation |
| Kubernetes cluster takeover | namespaced Role, dedicated ServiceAccount, restricted pods, no hostPath/NodePort |
| Compromised workload | non-root, seccomp, dropped capabilities, read-only root, resource limits, NetworkPolicy |

## Residual risks

- The API database account determines which relations the read-only browser can see. Use a least-privileged account for public demos.
- An allowlisted routine can intentionally mutate data or perform external work. Review routine bodies, revoke PostgreSQL's default `PUBLIC` execute grants where appropriate, and grant the API role `EXECUTE` only on routines intended for REST exposure.
- A routine or argument rename is an intentional breaking change to its REST contract. A type change may alter automatic overload selection; catalog-driven clients refresh metadata and hand-written clients version such changes like any other API contract.
- A malicious file matching an allowed signature may still be harmful when opened. Production deployments should add malware scanning and isolated object storage.
- The example NetworkPolicy permits TCP 443 and PostgreSQL 5432 egress broadly because database/API CIDRs vary. Narrow these ranges in production.
- Access tokens live in browser `sessionStorage`; an XSS would be able to read them. The frontend CSP, dependency audits and lack of raw HTML reduce but do not eliminate that risk.
- Audit retention is a data-minimization control, not a compliance archive. Export immutable security events to a dedicated system if regulation requires it.
- Secrets formerly committed to Git must be considered compromised even after removal from the current tree.
