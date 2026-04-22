# Убирает ** в заголовках (# … **текст**) для единообразия в Obsidian.
# Не трогает fenced-блоки ```…```.

from __future__ import annotations

import re
import sys
from pathlib import Path

PAT = re.compile(r"^(#{1,6}) \*\*(.+?)\*\*\s*$", re.MULTILINE)


def process(text: str) -> str:
    out: list[str] = []
    in_fence = False
    for line in text.splitlines(keepends=True):
        if line.strip().startswith("```"):
            in_fence = not in_fence
            out.append(line)
            continue
        if not in_fence:
            m = re.match(r"^(#{1,6}) \*\*(.+?)\*\*\s*(\r?\n)?$", line)
            if m:
                suf = m.group(3) or ""
                line = f"{m.group(1)} {m.group(2)}{suf}"
        out.append(line)
    return "".join(out)


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    names = [
        "Docs-Index.md",
        "DTO Description.md",
        "Entities.md",
        "polyraspad-frontend-backend-overview.md",
        "react-nextjs-by-project.md",
        "Storage-MinIO-Setup.md",
        "Информационную Архитектуру (IA).md",
        "Описание gRPC.md",
        "Описание REST API.md",
        "Основные возможности.md",
        "Ревизия-пайплайн-обучения-FSRS.md",
    ]
    for n in names:
        p = root / n
        if not p.is_file():
            print("skip", p, file=sys.stderr)
            continue
        old = p.read_text(encoding="utf-8")
        new = process(old)
        if new != old:
            p.write_text(new, encoding="utf-8")
            print("ok", n)
        else:
            print("—", n)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
