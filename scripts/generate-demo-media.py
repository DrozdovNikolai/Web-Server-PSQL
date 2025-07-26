#!/usr/bin/env python3
"""Capture the live Compose API and render portfolio media from its responses."""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError as error:  # pragma: no cover - local documentation helper
    raise SystemExit("Pillow is required: python3 -m pip install Pillow") from error


WIDTH = 1280
HEIGHT = 720
BACKGROUND = "#07111f"
SURFACE = "#0d1b2d"
SURFACE_2 = "#12243a"
BORDER = "#263c55"
TEXT = "#eef6ff"
MUTED = "#8ea5bd"
GREEN = "#61e6aa"
CYAN = "#55d7ff"
PURPLE = "#aa8cff"
YELLOW = "#ffd166"
RED = "#ff7b8b"


def font_path(name: str) -> str:
    candidates = {
        "sans": [
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
        ],
        "bold": [
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
            "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf",
        ],
        "mono": [
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf",
            "/usr/share/fonts/truetype/liberation2/LiberationMono-Regular.ttf",
        ],
        "mono_bold": [
            "/usr/share/fonts/truetype/dejavu/DejaVuSansMono-Bold.ttf",
            "/usr/share/fonts/truetype/liberation2/LiberationMono-Bold.ttf",
        ],
    }
    for candidate in candidates[name]:
        if Path(candidate).exists():
            return candidate
    raise SystemExit(f"No suitable {name} font found")


FONTS = {
    "title": ImageFont.truetype(font_path("bold"), 32),
    "subtitle": ImageFont.truetype(font_path("sans"), 17),
    "heading": ImageFont.truetype(font_path("bold"), 23),
    "body": ImageFont.truetype(font_path("sans"), 18),
    "small": ImageFont.truetype(font_path("sans"), 14),
    "badge": ImageFont.truetype(font_path("bold"), 13),
    "mono": ImageFont.truetype(font_path("mono"), 17),
    "mono_small": ImageFont.truetype(font_path("mono"), 14),
    "mono_bold": ImageFont.truetype(font_path("mono_bold"), 17),
    "hero": ImageFont.truetype(font_path("bold"), 42),
}


class ApiClient:
    def __init__(self, base_url: str, username: str, password: str) -> None:
        self.base_url = base_url.rstrip("/")
        self.username = username
        self.password = password
        self.token = ""

    def _request(self, path: str, payload: object | None = None) -> tuple[int, dict[str, str], bytes]:
        data = None
        headers = {"Accept": "application/json"}
        if payload is not None:
            data = json.dumps(payload).encode("utf-8")
            headers["Content-Type"] = "application/json"
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        request = urllib.request.Request(
            f"{self.base_url}/{path.lstrip('/')}", data=data, headers=headers
        )
        try:
            with urllib.request.urlopen(request, timeout=15) as response:
                return response.status, dict(response.headers.items()), response.read()
        except urllib.error.HTTPError as error:
            return error.code, dict(error.headers.items()), error.read()

    def json(self, path: str, payload: object | None = None, expected: int = 200) -> object:
        status, _, body = self._request(path, payload)
        parsed = json.loads(body.decode("utf-8")) if body else None
        if status != expected:
            raise RuntimeError(f"{path} returned HTTP {status}: {parsed}")
        return parsed

    def login(self) -> None:
        response = self.json(
            "auth/login",
            {"username": self.username, "password": self.password},
        )
        if not isinstance(response, dict) or not isinstance(response.get("accessToken"), str):
            raise RuntimeError("Login response did not contain an access token")
        self.token = response["accessToken"]


def capture(base_url: str, username: str, password: str, output_dir: Path) -> dict[str, object]:
    client = ApiClient(base_url, username, password)
    client.login()

    catalog = client.json("database/routines?schema=public")
    if not isinstance(catalog, list):
        raise RuntimeError("Routine catalog was not an array")

    format_signatures = sorted(
        item["signature"]
        for item in catalog
        if isinstance(item, dict) and item.get("name") == "format_value"
    )
    integer_result = client.json(
        "database/routines/public/format_value/invoke",
        {"arguments": {"value": 7}},
    )
    text_result = client.json(
        "database/routines/public/format_value/invoke",
        {"arguments": {"value": "hello"}},
    )
    cursor_rows = client.json(
        "database/routines/public/open_demo_orders_report/invoke",
        {"arguments": {"minimum_subtotal": 100}},
    )

    ambiguity_status, _, ambiguity_body = client._request(
        "database/routines/public/format_value/invoke",
        {"arguments": {"value": None}},
    )
    ambiguity = json.loads(ambiguity_body.decode("utf-8"))
    if ambiguity_status != 409:
        raise RuntimeError(f"Expected ambiguity HTTP 409, received {ambiguity_status}")

    export_payload = {
        "arguments": {"minimum_subtotal": 100},
        "excel": {
            "fileName": "orders-report.xlsx",
            "sheetName": "Orders",
            "reportStartPosition": {"row": 2, "column": 2},
            "autoFitColumns": True,
            "freezePane": {"rows": 2, "columns": 1},
            "columnWidths": {"0": 18},
            "rowHeights": {"0": 24},
            "ranges": [],
            "cells": [],
        },
    }
    export_status, export_headers, workbook = client._request(
        "database/routines/public/open_demo_orders_report/export/xlsx",
        export_payload,
    )
    if export_status != 200 or not workbook.startswith(b"PK"):
        raise RuntimeError(f"Excel export returned HTTP {export_status} or an invalid workbook")

    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / "orders-report.xlsx").write_bytes(workbook)

    return {
        "format_signatures": format_signatures,
        "integer_result": integer_result,
        "text_result": text_result,
        "cursor_rows": cursor_rows,
        "ambiguity_candidates": ambiguity.get("candidates", []),
        "workbook_size": len(workbook),
        "workbook_type": export_headers.get(
            "Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ).split(";", 1)[0],
    }


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGB", (WIDTH, HEIGHT), BACKGROUND)
    draw = ImageDraw.Draw(image)
    for x in range(0, WIDTH, 48):
        draw.line((x, 0, x, HEIGHT), fill="#0b1929", width=1)
    for y in range(0, HEIGHT, 48):
        draw.line((0, y, WIDTH, y), fill="#0b1929", width=1)
    draw.ellipse((1050, -180, 1450, 220), fill="#0b2033")
    draw.ellipse((-210, 560, 210, 980), fill="#111b35")
    return image, draw


def database_icon(draw: ImageDraw.ImageDraw, x: int, y: int) -> None:
    draw.ellipse((x, y, x + 34, y + 12), fill=CYAN)
    draw.rectangle((x, y + 6, x + 34, y + 29), fill=CYAN)
    draw.arc((x, y + 18, x + 34, y + 31), 0, 180, fill=BACKGROUND, width=2)
    draw.ellipse((x, y + 23, x + 34, y + 35), fill=CYAN)


def badge(draw: ImageDraw.ImageDraw, x: int, y: int, label: str, color: str) -> int:
    bounds = draw.textbbox((0, 0), label, font=FONTS["badge"])
    text_width = bounds[2] - bounds[0]
    text_height = bounds[3] - bounds[1]
    height = 28
    padding = 12
    dot_size = 8
    gap = 6
    width = padding * 2 + dot_size + gap + text_width
    dot_x = x + padding
    dot_y = y + (height - dot_size) // 2
    text_x = dot_x + dot_size + gap - bounds[0]
    text_y = y + (height - text_height) // 2 - bounds[1]
    draw.rounded_rectangle((x, y, x + width, y + height), radius=14, fill=SURFACE_2, outline=color)
    draw.ellipse((dot_x, dot_y, dot_x + dot_size, dot_y + dot_size), fill=color)
    draw.text((text_x, text_y), label, font=FONTS["badge"], fill=TEXT)
    return x + width


def header(draw: ImageDraw.ImageDraw, kicker: str) -> None:
    database_icon(draw, 52, 42)
    draw.text((100, 37), "PostgreSQL Routine Gateway", font=FONTS["title"], fill=TEXT)
    draw.text((101, 80), kicker, font=FONTS["subtitle"], fill=MUTED)
    x = badge(draw, 922, 45, "LIVE API", GREEN)
    badge(draw, x + 12, 45, ".NET 10", PURPLE)


def window(draw: ImageDraw.ImageDraw, title: str) -> None:
    draw.rounded_rectangle((48, 130, 1232, 632), radius=20, fill=SURFACE, outline=BORDER, width=2)
    draw.rounded_rectangle((48, 130, 1232, 181), radius=20, fill="#102238")
    draw.rectangle((48, 161, 1232, 181), fill="#102238")
    for index, color in enumerate((RED, YELLOW, GREEN)):
        draw.ellipse((70 + index * 24, 149, 82 + index * 24, 161), fill=color)
    draw.text((162, 146), title, font=FONTS["small"], fill=MUTED)


def panel(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    label: str,
    accent: str = CYAN,
) -> None:
    draw.rounded_rectangle(box, radius=14, fill=SURFACE_2, outline=BORDER)
    x1, y1, _, _ = box
    draw.text((x1 + 20, y1 + 16), label.upper(), font=FONTS["badge"], fill=accent)


def code_lines(
    draw: ImageDraw.ImageDraw,
    x: int,
    y: int,
    lines: list[tuple[str, str]],
    spacing: int = 27,
    font: str = "mono",
) -> None:
    for offset, (value, color) in enumerate(lines):
        draw.text((x, y + offset * spacing), value, font=FONTS[font], fill=color)


def footer(draw: ImageDraw.ImageDraw, step: int, total: int, label: str) -> None:
    draw.text((50, 666), label, font=FONTS["small"], fill=MUTED)
    start = 1088
    for index in range(total):
        color = GREEN if index == step else BORDER
        width = 28 if index == step else 10
        draw.rounded_rectangle((start, 671, start + width, 681), radius=5, fill=color)
        start += width + 10


def cover_frame(data: dict[str, object], step: int, total: int) -> Image.Image:
    image, draw = canvas()
    header(draw, "Stored PostgreSQL functions and procedures, exposed safely over REST")
    window(draw, "The business idea — preserved and production-ready")
    draw.text((84, 218), "One endpoint.", font=FONTS["hero"], fill=TEXT)
    draw.text((84, 270), "Zero PostgreSQL type guessing on the client.", font=FONTS["hero"], fill=GREEN)
    draw.text(
        (86, 340),
        "Send named JSON arguments. The server discovers signatures in pg_catalog,",
        font=FONTS["body"],
        fill=MUTED,
    )
    draw.text(
        (86, 370),
        "resolves overloads, binds values, and returns the routine result directly.",
        font=FONTS["body"],
        fill=MUTED,
    )
    panel(draw, (84, 432, 394, 574), "REST request", CYAN)
    panel(draw, (414, 432, 724, 574), "Catalog resolver", PURPLE)
    panel(draw, (744, 432, 1196, 574), "Result", GREEN)
    code_lines(draw, 105, 479, [("{ named JSON }", TEXT), ("no type metadata", MUTED)], font="mono_small")
    code_lines(draw, 435, 479, [("pg_catalog", TEXT), (f"{len(data['format_signatures'])} overloads found", MUTED)], font="mono_small")
    code_lines(draw, 765, 479, [("scalar · rows · refcursor", TEXT), ("JSON or real .xlsx", GREEN)], font="mono_small")
    footer(draw, step, total, "Live walkthrough captured from the local Compose stack")
    return image


def overload_frame(
    value_json: str,
    result: str,
    signature: str,
    value_kind: str,
    step: int,
    total: int,
) -> Image.Image:
    image, draw = canvas()
    header(draw, "Automatic overload resolution from ordinary JSON")
    window(draw, "POST /server/api/database/routines/public/format_value/invoke")
    panel(draw, (76, 207, 614, 588), "Request", CYAN)
    code_lines(
        draw,
        102,
        258,
        [
            ("{", MUTED),
            ('  "arguments": {', TEXT),
            (f'    "value": {value_json}', YELLOW),
            ("  }", TEXT),
            ("}", MUTED),
        ],
        spacing=31,
    )
    draw.line((102, 450, 585, 450), fill=BORDER, width=1)
    draw.text((102, 472), "No OID. No identityArguments. No type field.", font=FONTS["small"], fill=MUTED)

    panel(draw, (638, 207, 1204, 588), "Server decision", PURPLE)
    badge(draw, 666, 256, "AUTO-RESOLVED", GREEN)
    draw.text((666, 310), "JSON kind", font=FONTS["small"], fill=MUTED)
    draw.text((835, 306), value_kind, font=FONTS["mono_bold"], fill=YELLOW)
    draw.text((666, 353), "Selected", font=FONTS["small"], fill=MUTED)
    draw.text((835, 349), signature, font=FONTS["mono_bold"], fill=CYAN)
    draw.line((666, 403, 1174, 403), fill=BORDER, width=1)
    draw.text((666, 431), "200 OK", font=FONTS["badge"], fill=GREEN)
    draw.rounded_rectangle((666, 465, 1174, 535), radius=10, fill="#091725")
    draw.text((688, 488), json.dumps(result), font=FONTS["mono_bold"], fill=TEXT)
    footer(draw, step, total, "Same URL, different PostgreSQL overload — selected by the server")
    return image


def cursor_frame(data: dict[str, object], step: int, total: int) -> Image.Image:
    rows = data["cursor_rows"]
    if not isinstance(rows, list):
        raise RuntimeError("Cursor response was not an array")
    image, draw = canvas()
    header(draw, "A refcursor becomes a bounded JSON result in one transaction")
    window(draw, "POST /server/api/database/routines/public/open_demo_orders_report/invoke")
    panel(draw, (76, 207, 472, 588), "Named arguments", CYAN)
    code_lines(
        draw,
        100,
        262,
        [
            ("{", MUTED),
            ('  "arguments": {', TEXT),
            ('    "minimum_subtotal": 100', YELLOW),
            ("  }", TEXT),
            ("}", MUTED),
        ],
        spacing=31,
        font="mono_small",
    )
    draw.text((100, 456), "BEGIN", font=FONTS["mono_bold"], fill=PURPLE)
    draw.text((100, 489), "call → FETCH ALL → COMMIT", font=FONTS["mono_small"], fill=MUTED)

    panel(draw, (496, 207, 1204, 588), f"JSON response · {len(rows)} rows", GREEN)
    columns = ["order_id", "customer_name", "subtotal"]
    widths = [120, 320, 170]
    x = 523
    y = 270
    for column, width in zip(columns, widths):
        draw.rounded_rectangle((x, y, x + width, y + 42), radius=6, fill="#192f48")
        draw.text((x + 12, y + 12), column, font=FONTS["mono_small"], fill=CYAN)
        x += width + 8
    for row_index, row in enumerate(rows[:4]):
        if not isinstance(row, dict):
            continue
        x = 523
        y = 321 + row_index * 58
        for column, width in zip(columns, widths):
            draw.rounded_rectangle((x, y, x + width, y + 48), radius=6, fill="#0b1929")
            draw.text((x + 12, y + 14), str(row.get(column, "")), font=FONTS["mono_small"], fill=TEXT)
            x += width + 8
    draw.text((523, 474), "Direct routine output — no response envelope", font=FONTS["small"], fill=MUTED)
    footer(draw, step, total, "The gateway keeps the cursor open only inside the server transaction")
    return image


def excel_frame(data: dict[str, object], step: int, total: int) -> Image.Image:
    size = int(data["workbook_size"])
    image, draw = canvas()
    header(draw, "The same refcursor can become a configured Excel workbook")
    window(draw, "POST /server/api/database/routines/public/open_demo_orders_report/export/xlsx")
    panel(draw, (76, 207, 644, 588), "Request", CYAN)
    code_lines(
        draw,
        98,
        252,
        [
            ('"arguments": { "minimum_subtotal": 100 },', TEXT),
            ('"excel": {', TEXT),
            ('  "fileName": "orders-report.xlsx",', YELLOW),
            ('  "sheetName": "Orders",', YELLOW),
            ('  "reportStartPosition": { "row": 2, "column": 2 },', TEXT),
            ('  "autoFitColumns": true,', TEXT),
            ('  "freezePane": { "rows": 2, "columns": 1 }', TEXT),
            ('}', TEXT),
        ],
        spacing=31,
        font="mono_small",
    )

    panel(draw, (668, 207, 1204, 588), "Download", GREEN)
    draw.rounded_rectangle((706, 258, 1166, 518), radius=18, fill="#0a1928", outline="#2e5a52", width=2)
    draw.rounded_rectangle((750, 304, 832, 404), radius=10, fill="#16764a")
    draw.rectangle((770, 284, 850, 383), fill="#23a566")
    draw.text((790, 311), "X", font=FONTS["hero"], fill="white")
    draw.text((880, 300), "orders-report.xlsx", font=FONTS["heading"], fill=TEXT)
    draw.text((880, 343), f"{size:,} bytes · valid ZIP workbook", font=FONTS["small"], fill=MUTED)
    badge(draw, 880, 382, "200 OK", GREEN)
    draw.text((738, 459), "Real workbook generated by ClosedXML from live cursor rows", font=FONTS["small"], fill=MUTED)
    footer(draw, step, total, "The generated workbook is attached next to this demo")
    return image


def ambiguity_frame(data: dict[str, object], step: int, total: int) -> Image.Image:
    candidates = data["ambiguity_candidates"]
    count = len(candidates) if isinstance(candidates, list) else 0
    image, draw = canvas()
    header(draw, "Ambiguity is explicit — the client only sends a hint when needed")
    window(draw, "POST /server/api/database/routines/public/format_value/invoke")
    panel(draw, (76, 207, 534, 588), "Ambiguous request", CYAN)
    code_lines(
        draw,
        102,
        265,
        [
            ("{", MUTED),
            ('  "arguments": {', TEXT),
            ('    "value": null', YELLOW),
            ("  }", TEXT),
            ("}", MUTED),
        ],
        spacing=31,
    )
    draw.text((102, 461), "null carries no JSON type information", font=FONTS["small"], fill=MUTED)

    panel(draw, (558, 207, 1204, 588), "409 Problem Details", RED)
    draw.text((586, 260), f"{count} equally compatible overloads", font=FONTS["heading"], fill=TEXT)
    y = 316
    if isinstance(candidates, list):
        for candidate in candidates[:3]:
            if not isinstance(candidate, dict):
                continue
            signature = str(candidate.get("signature", candidate.get("overload", "unknown")))
            draw.rounded_rectangle((586, y, 1176, y + 50), radius=9, fill="#0a1928")
            draw.text((606, y + 14), signature, font=FONTS["mono_small"], fill=CYAN)
            y += 62
    draw.text((586, 502), 'Escape hatch: "overload": "integer"', font=FONTS["mono_small"], fill=GREEN)
    footer(draw, step, total, "Routine metadata remains a server concern in the normal product flow")
    return image


def save_media(data: dict[str, object], output_dir: Path) -> None:
    total = 6
    signatures = data["format_signatures"]
    integer_signature = next(
        (value for value in signatures if "integer" in value), "format_value(integer)"
    )
    text_signature = next((value for value in signatures if "text" in value), "format_value(text)")
    frames = [
        cover_frame(data, 0, total),
        overload_frame("7", str(data["integer_result"]), integer_signature, "integer", 1, total),
        overload_frame(
            '"hello"', str(data["text_result"]), text_signature, "string", 2, total
        ),
        ambiguity_frame(data, 3, total),
        cursor_frame(data, 4, total),
        excel_frame(data, 5, total),
    ]

    poster = frames[0].copy()
    poster.save(output_dir / "routine-gateway-demo.png", optimize=True)

    paletted = [frame.quantize(colors=128, method=Image.Quantize.MEDIANCUT) for frame in frames]
    paletted[0].save(
        output_dir / "routine-gateway-demo.gif",
        save_all=True,
        append_images=paletted[1:],
        duration=[1800, 2200, 2200, 2400, 2600, 2800],
        loop=0,
        optimize=True,
        disposal=2,
    )


def main() -> int:
    base_url = os.getenv("DEMO_BASE_URL", "http://127.0.0.1:5050/server/api")
    username = os.getenv("DEMO_USERNAME", "admin")
    password = os.getenv("DEMO_PASSWORD")
    output_dir = Path(os.getenv("DEMO_OUTPUT_DIR", "docs/media"))
    if not password:
        print("DEMO_PASSWORD is required; use the disposable local Compose credential.", file=sys.stderr)
        return 2

    data = capture(base_url, username, password, output_dir)
    save_media(data, output_dir)
    print(
        json.dumps(
            {
                "gif": str(output_dir / "routine-gateway-demo.gif"),
                "poster": str(output_dir / "routine-gateway-demo.png"),
                "workbook": str(output_dir / "orders-report.xlsx"),
                "captured": data,
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
