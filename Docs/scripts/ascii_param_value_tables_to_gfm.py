# -*- coding: utf-8 -*-
"""Конвертация ASCII-таблиц «Параметр / Значение» (двухколоночных) в GFM."""
from __future__ import annotations

import re
import sys
from pathlib import Path


DASH_LINE = re.compile(r"^  -{20,}\s*$")
DATA_ROW = re.compile(r"^  (\*\*[^*]+\*\*)\s+(.+?)\s*$")


def escape_cell(s: str) -> str:
    s = s.replace("\\[", "[").replace("\\]", "]")
    s = s.replace("|", "\\|")
    return s


def parse_block(lines: list[str], start: int) -> tuple[int, str] | None:
    if start >= len(lines) or not DASH_LINE.match(lines[start]):
        return None
    if start + 2 >= len(lines):
        return None
    header = lines[start + 1]
    if "Параметр" not in header or "Значение" not in header:
        return None
    if not re.match(r"^  -+", lines[start + 2]):
        return None
    j = start + 3
    rows: list[tuple[str, str]] = []
    while j < len(lines):
        line = lines[j]
        if DASH_LINE.match(line):
            out = ["| Параметр | Значение |", "| --- | --- |"]
            for k, v in rows:
                out.append(f"| {k} | {escape_cell(v)} |")
            return j + 1, "\n".join(out) + "\n"
        if not line.strip():
            j += 1
            continue
        m = DATA_ROW.match(line)
        if m:
            rows.append((m.group(1), m.group(2)))
        j += 1
    return None


def main() -> int:
    path = Path(__file__).resolve().parent.parent / "Описание REST API.md"
    if len(sys.argv) > 1:
        path = Path(sys.argv[1])
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    out: list[str] = []
    i = 0
    while i < len(lines):
        parsed = parse_block(lines, i)
        if parsed is not None:
            new_i, gfm = parsed
            out.append(gfm)
            i = new_i
        else:
            out.append(lines[i])
            i += 1
    path.write_text("".join(out), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
