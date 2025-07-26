# Demo media

The animation and workbook in this directory are generated from the real local API. The media script authenticates against the disposable Docker Compose environment, reads the routine catalog, invokes both `format_value` overloads without a client-side type hint, captures the expected ambiguity response, fetches `open_demo_orders_report` as JSON and downloads its Excel export.

No access token or credential is written to an artifact. `orders-report.xlsx` is the unmodified workbook returned by the API; the PNG and GIF render only the captured request/result data.

## Reproduce

Requirements: the repository's Docker Compose stack, Python 3 and Pillow.

```bash
docker compose --env-file .env up -d --build
DEMO_PASSWORD='<BOOTSTRAP_ADMIN_PASSWORD from .env>' \
  python3 scripts/generate-demo-media.py
```

Optional environment variables:

- `DEMO_BASE_URL` defaults to `http://127.0.0.1:5050/server/api`.
- `DEMO_USERNAME` defaults to `admin`.
- `DEMO_OUTPUT_DIR` defaults to `docs/media`.

The script fails if overload selection, cursor invocation, ambiguity handling or workbook generation no longer behaves as shown, so regenerating the media is also a small end-to-end smoke test of the project's central feature.
