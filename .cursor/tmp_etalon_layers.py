# -*- coding: utf-8 -*-
"""Etalon completeness vs Auth 04 structure."""
from pathlib import Path

ROOT = Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")

LAYERS = [
    "Методы API/gRPC",
    "Методы API/DTO",
    "Методы API/REST API",
    "Методы API/Socket",
    "Интеграции со сторонними сервисами",
    "Алгоритмы и методы бекенда",
    "Работа с Redis",
    "Работа с Rabbit MQ",
]

SERVICES = {
    "(Done) Authorization Service": "etalon",
    "Aggregator Service": "BFF",
    "Agent Service": "gRPC-only",
    "Billing Service": "gRPC",
    "Media Service": "gRPC",
    "Authorization Module": "REST+gRPC",
}


def layer_count(svc: str, layer: str) -> int:
    p = ROOT / svc / "04 - Бекенд, API и Контракты" / layer
    if not p.exists():
        return -1
    return len(list(p.glob("*.md"))) + len(list(p.glob("*.proto")))


print("Layer comparison (md+proto count, -1 = N/A folder missing)")
print(f"{'Service':<35}", end="")
for layer in LAYERS:
    short = layer.split("/")[-1][:8]
    print(f"{short:>10}", end="")
print()
print("-" * 120)
for svc, note in SERVICES.items():
    print(f"{svc[:34]:<35}", end="")
    for layer in LAYERS:
        c = layer_count(svc, layer)
        cell = str(c) if c >= 0 else "—"
        print(f"{cell:>10}", end="")
    print(f"  ({note})")
