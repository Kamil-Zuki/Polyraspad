# Вставляет пустую строку после ## / ###, если сразу идёт текст.

import re
from pathlib import Path

PAT = re.compile(
    r"^((?:#{2,3}) [^\n]+)\n"
    r"(?=(?:[A-Za-zА-Яа-яЁё0-9*]|\d+\. ))",
    re.MULTILINE,
)

NAMES = [
    "DTO Description.md",
    "Entities.md",
    "Описание gRPC.md",
    "Описание REST API.md",
    "Основные возможности.md",
    "Информационную Архитектуру (IA).md",
]

if __name__ == "__main__":
    root = Path(__file__).resolve().parents[1]
    for n in NAMES:
        p = root / n
        if not p.is_file():
            continue
        t = p.read_text(encoding="utf-8")
        if t.startswith("---\n"):
            m = re.match(r"^---\n.*?\n---\n", t, re.DOTALL)
            if m:
                head, rest = m.group(0), t[m.end() :]
                rest = PAT.sub(r"\1\n\n", rest)
                t = head + rest
        else:
            t = PAT.sub(r"\1\n\n", t)
        p.write_text(t, encoding="utf-8")
        print("ok", n)
