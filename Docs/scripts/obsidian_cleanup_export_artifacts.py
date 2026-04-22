# Скрипт: убирает артефакты экспорта, оборачивает «Пример JSON» в ```json (Obsidian).
# Запуск: python Docs/scripts/obsidian_cleanup_export_artifacts.py

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ARTIFACTS = {
    "code json",
    "code sql",
    "downloadcontent_copy",
    "expand_less",
}


def is_artifact_line(line: str) -> bool:
    return line.strip().lower() in ARTIFACTS


def strip_artifact_lines(text: str) -> str:
    return "".join(
        line
        for line in text.splitlines(keepends=True)
        if not is_artifact_line(line)
    )


def normalize_export_json(s: str) -> str:
    t = re.sub(r"[\n\r\t]+", " ", s.strip())
    t = re.sub(r" +", " ", t)
    t = t.replace("\\[", "[").replace("\\]", "]")
    t = t.replace("\\<", "<").replace("\\>", ">")
    return t.replace('\\"', '"')


def find_json_object_end_in_text(from_brace: str) -> int:
    # Тестируем только индексы, где в исходнике «}» — сильно быстрее на крупных файлах
    for hi in (i + 1 for i, c in enumerate(from_brace) if c == "}"):
        if not from_brace[:hi].rstrip().endswith("}"):
            continue
        sub1 = normalize_export_json(from_brace[:hi])
        try:
            _v, e = json.JSONDecoder().raw_decode(sub1)
        except json.JSONDecodeError:
            continue
        if e == len(sub1) or not sub1[e:].strip():
            return hi
    return 0


def try_format_json(js_raw: str) -> str:
    s1 = normalize_export_json(js_raw)
    try:
        v = json.loads(s1)
        return json.dumps(v, ensure_ascii=False, indent=2)
    except json.JSONDecodeError:
        return js_raw


def process_file(path: Path) -> None:
    text = strip_artifact_lines(path.read_text(encoding="utf-8"))
    out: list[str] = []
    pos = 0
    for m in re.finditer(r"\*\*Пример JSON[^\n]*\*\*", text):
        out.append(text[pos : m.end()])
        rest = text[m.end() :]
        b = rest.find("{")
        if b < 0:
            pos = m.end()
            continue
        from_brace = rest[b:]
        end_len = find_json_object_end_in_text(from_brace)
        if end_len <= 0:
            pos = m.end()
            continue
        pretty = try_format_json(from_brace[:end_len])
        out.append("\n\n```json\n")
        out.append(pretty)
        out.append("\n```\n\n")
        pos = m.end() + b + end_len
    out.append(text[pos:])
    path.write_text("".join(out), encoding="utf-8")


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    for name in ("DTO Description.md", "Entities.md"):
        p = root / name
        if p.is_file():
            process_file(p)
            print("ok:", p.name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
