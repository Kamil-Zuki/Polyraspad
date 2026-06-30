# -*- coding: utf-8 -*-
"""Полнота folder 04 vs эталон Auth — группы 01 vs файлы 04."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Docs"
AUTH = ROOT / "(Done) Authorization Service"

SERVICES = [
    "Aggregator Service",
    "Agent Service",
    "Billing Service",
    "Authorization Module",
    "Media Service",
]


def groups_in_01(svc):
    z = ROOT / svc / "01 - Функциональная спецификация" / "Возможности сервиса" / "00 - Общая информация.md"
    return len(re.findall(r"^### \d+\.", z.read_text(encoding="utf-8"), re.M))


def count_sub(d, sub):
    p = d / sub
    return len(list(p.rglob("*.md"))) if p.exists() else 0


def grpc_groups(d):
    g = d / "Методы API/gRPC"
    if not g.exists():
        return 0
    return len([f for f in g.glob("*.md") if f.name != "00 - gRPC - Общая информация.md" and not f.suffix == ".proto"])


def rest_groups(d):
    r = d / "Методы API/REST API"
    if not r.exists():
        return 0
    return len([f for f in r.glob("*.md") if not f.name.startswith("00")])


def dto_groups(d):
    dto = d / "Методы API/DTO"
    if not dto.exists():
        return 0
    return len([f for f in dto.glob("*.md") if not f.name.startswith("00")])


def algo_groups(d):
    a = d / "Алгоритмы и методы бекенда"
    if not a.exists():
        return 0
    return len([f for f in a.glob("*.md") if not f.name.startswith("00")])


def integ_files(d):
    i = d / "Интеграции со сторонними сервисами"
    return len(list(i.glob("*.md"))) if i.exists() else 0


print("=== Done Auth etalon (reference) ===")
a04 = AUTH / "04 - Бекенд, API и Контракты"
print(f"04 total md: {len(list(a04.rglob('*.md')))}")
print(f"  gRPC groups: {grpc_groups(a04)} | REST: {rest_groups(a04)} | DTO: {dto_groups(a04)} | algo: {algo_groups(a04)} | integrations: {integ_files(a04)}")
print(f"  Rabbit: {count_sub(a04, 'Работа с Rabbit MQ')} | Redis: {count_sub(a04, 'Работа с Redis')} | Socket: {count_sub(a04, 'Методы API/Socket')}")
print(f"05 md: {len(list((AUTH / '05 - Сводная документация').rglob('*.md')))}")

print("\n=== Volume gap: 01 groups vs 04 contract layers ===")
for svc in SERVICES:
    d = ROOT / svc / "04 - Бекенд, API и Контракты"
    g01 = groups_in_01(svc)
    if not d.exists():
        print(f"{svc}: NO 04 | 01 groups={g01}")
        continue
    total = len(list(d.rglob("*.md")))
    has05 = (ROOT / svc / "05 - Сводная документация").exists()
    print(
        f"{svc}: 01_groups={g01} | 04_md={total} | gRPC_grp={grpc_groups(d)} REST_grp={rest_groups(d)} "
        f"DTO_grp={dto_groups(d)} algo_grp={algo_groups(d)} integ={integ_files(d)} | 05={'yes' if has05 else 'NO'}"
    )
