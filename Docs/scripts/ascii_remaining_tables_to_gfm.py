# -*- coding: utf-8 -*-
"""
Конвертация оставшихся ASCII-таблиц:
- «Название параметра | Тип данных | Описание» (параметры URL);
- «Метод | Эндпоинт | Описание» (сводные блоки).
"""
from __future__ import annotations

import re
import sys
from pathlib import Path


DASH_LINE = re.compile(r"^  -{12,}\s*$")
METHOD_ROW = re.compile(
    r"^  (GET|POST|PUT|DELETE|PATCH)\s+(\S+)\s+(.+?)\s*$"
)


def esc(s: str) -> str:
    return s.replace("|", "\\|")


def _parse_param_triplet_table(
    lines: list[str],
    start: int,
    h1: str,
    h2: str,
    h3: str,
) -> tuple[int, str] | None:
    if start + 2 >= len(lines) or not DASH_LINE.match(lines[start]):
        return None
    h = lines[start + 1]
    if h1 not in h or h2 not in h or h3 not in h:
        return None
    if not re.match(r"^  -+", lines[start + 2]):
        return None
    j = start + 3
    rows: list[tuple[str, str, str]] = []
    while j < len(lines):
        line = lines[j]
        if DASH_LINE.match(line):
            gfm = [
                f"| {h1} | {h2} | {h3} |",
                "| --- | --- | --- |",
            ]
            for a, b, c in rows:
                gfm.append(f"| {esc(a)} | {esc(b)} | {esc(c)} |")
            return j + 1, "\n".join(gfm) + "\n"
        s = line.strip()
        if not s:
            j += 1
            continue
        parts = re.split(r" {2,}", s)
        if len(parts) >= 3:
            rows.append((parts[0], parts[1], " ".join(parts[2:])))
        j += 1
    return None


def parse_url_params(lines: list[str], start: int) -> tuple[int, str] | None:
    if start + 2 >= len(lines) or not DASH_LINE.match(lines[start]):
        return None
    h = lines[start + 1]
    if "Название параметра" in h and "Тип данных" in h:
        return _parse_param_triplet_table(
            lines, start, "Название параметра", "Тип данных", "Описание"
        )
    # Сокращённый заголовок: «Название   Тип   Описание»
    if re.search(r"\bНазвание\b", h) and re.search(
        r"(?<![\w-])Тип(?![\w-])", h
    ) and "Описание" in h and "Тип данных" not in h and "параметра" not in h:
        return _parse_param_triplet_table(lines, start, "Название", "Тип", "Описание")
    return None


def parse_method_endpoint(lines: list[str], start: int) -> tuple[int, str] | None:
    if start + 2 >= len(lines) or not DASH_LINE.match(lines[start]):
        return None
    h = lines[start + 1]
    if "Метод" not in h or "Эндпоинт" not in h or "Описание" not in h:
        return None
    if "Название параметра" in h:
        return None
    if not re.match(r"^  -+", lines[start + 2]):
        return None
    j = start + 3
    rows: list[tuple[str, str, str]] = []
    while j < len(lines):
        line = lines[j]
        if DASH_LINE.match(line):
            gfm = [
                "| Метод | Эндпоинт | Описание |",
                "| --- | --- | --- |",
            ]
            for a, b, c in rows:
                gfm.append(f"| {esc(a)} | {esc(b)} | {esc(c)} |")
            return j + 1, "\n".join(gfm) + "\n"
        if not line.strip():
            j += 1
            continue
        m = METHOD_ROW.match(line)
        if m:
            rows.append((m.group(1), m.group(2), m.group(3).strip()))
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
        p = parse_url_params(lines, i)
        if p is None:
            p = parse_method_endpoint(lines, i)
        if p is not None:
            new_i, gfm = p
            out.append(gfm)
            i = new_i
        else:
            out.append(lines[i])
            i += 1
    path.write_text("".join(out), encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
