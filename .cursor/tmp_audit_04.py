# -*- coding: utf-8 -*-
import re
from pathlib import Path

ROOT = Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")
SERVICES = [
    "Aggregator Service",
    "Agent Service",
    "Billing Service",
    "Media Service",
    "Authorization Module",
]


def count_md(folder: Path) -> int:
    if not folder.exists():
        return 0
    return len(list(folder.rglob("*.md")))


def rest_detail_blocks(path: Path):
    if not path.exists():
        return 0
    text = path.read_text(encoding="utf-8")
    return len(re.findall(r"^# SR-AGG-", text, re.M))


print("=== 04 folder counts ===")
for svc in SERVICES:
    base = ROOT / svc / "04 - Бекенд, API и Контракты"
    print(f"{svc}: {count_md(base)} md files")

print()
print("=== Aggregator REST detail blocks ===")
rest = (
    ROOT
    / "Aggregator Service"
    / "04 - Бекенд, API и Контракты"
    / "Методы API"
    / "REST API"
)
gaps = []
for f in sorted(rest.glob("*.md")):
    if f.name.startswith("00"):
        continue
    d = rest_detail_blocks(f)
    flag = "OK" if d > 0 else "GAP"
    if d == 0:
        gaps.append(f.name)
    print(f"{flag} {f.name}: {d} detail blocks")
print(f"REST GAPs: {len(gaps)}")

print()
print("=== Agent gRPC ===")
grpc = (
    ROOT
    / "Agent Service"
    / "04 - Бекенд, API и Контракты"
    / "Методы API"
    / "gRPC"
)
for f in sorted(grpc.glob("*.md")):
    text = f.read_text(encoding="utf-8")
    anchors = len(re.findall(r'id="grpc-', text))
    blocks = len(re.findall(r"^## Общая информация", text, re.M))
    print(f"{f.name}: anchors={anchors}, blocks={blocks}")

proto = grpc / "agent.proto"
print(f"agent.proto exists: {proto.exists()}")
