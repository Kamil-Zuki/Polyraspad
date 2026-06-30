# -*- coding: utf-8 -*-
"""SR coverage in folder 04 vs 01 index."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Docs"
SERVICES = [
    "Aggregator Service",
    "Agent Service",
    "Billing Service",
    "Authorization Module",
    "Media Service",
]


def sr_from_00(svc):
    z = ROOT / svc / "01 - Функциональная спецификация" / "Возможности сервиса" / "00 - Общая информация.md"
    t = z.read_text(encoding="utf-8")
  # SR-XXX-YYY-NN
    return sorted(set(re.findall(r"SR-[A-Z]+-[A-Z0-9]+-\d+", t)))


def sr_in_04(svc):
    d = ROOT / svc / "04 - Бекенд, API и Контракты"
    if not d.exists():
        return set()
    text = ""
    for f in d.rglob("*.md"):
        text += f.read_text(encoding="utf-8") + "\n"
    return set(re.findall(r"SR-[A-Z]+-[A-Z0-9]+-\d+", text))


def main():
    for svc in SERVICES:
        idx = sr_from_00(svc)
        doc = sr_in_04(svc)
        missing = [s for s in idx if s not in doc]
        print(f"\n=== {svc} ===")
        print(f"01 index SR: {len(idx)} | referenced in 04: {len([s for s in idx if s in doc])}/{len(idx)}")
        if missing:
            print("MISSING in 04:", ", ".join(missing))


if __name__ == "__main__":
    main()
