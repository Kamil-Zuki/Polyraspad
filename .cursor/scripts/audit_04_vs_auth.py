# -*- coding: utf-8 -*-
"""Сравнение folder 04 с эталоном Auth — layout, не domain text."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Docs"
AUTH_04 = ROOT / "(Done) Authorization Service" / "04 - Бекенд, API и Контракты"

AUTH_SUBFOLDERS = {
    "Методы API/gRPC",
    "Методы API/DTO",
    "Методы API/REST API",
    "Методы API/Socket",
    "Интеграции со сторонними сервисами",
    "Работа с Rabbit MQ",
    "Работа с Redis",
    "Алгоритмы и методы бекенда",
}

SERVICES = {
    "Aggregator Service": {
        "expect": ["Методы API/DTO", "Методы API/REST API", "Интеграции со сторонними сервисами", "Алгоритмы и методы бекенда"],
        "skip": ["Методы API/gRPC", "Методы API/Socket", "Работа с Rabbit MQ", "Работа с Redis"],
    },
    "Media Service": {
        "expect": ["Методы API/gRPC", "Методы API/DTO", "Интеграции со сторонними сервисами", "Алгоритмы и методы бекенда"],
        "skip": ["Методы API/REST API", "Методы API/Socket", "Работа с Rabbit MQ", "Работа с Redis"],
    },
    "Billing Service": {
        "expect": ["Методы API/gRPC", "Интеграции со сторонними сервисами", "Алгоритмы и методы бекенда"],
        "skip": ["Методы API/REST API", "Методы API/Socket", "Работа с Rabbit MQ", "Работа с Redis"],
        "optional": ["Методы API/DTO"],
    },
    "Agent Service": {
        "expect": ["Методы API/gRPC", "Методы API/DTO", "Интеграции со сторонними сервисами", "Алгоритмы и методы бекенда"],
        "skip": ["Методы API/REST API", "Методы API/Socket", "Работа с Rabbit MQ", "Работа с Redis"],
    },
    "Authorization Module": {
        "expect": ["Методы API/gRPC", "Методы API/DTO", "Методы API/REST API", "Интеграции со сторонними сервисами", "Алгоритмы и методы бекенда"],
        "skip": ["Методы API/Socket", "Работа с Rabbit MQ", "Работа с Redis"],
    },
}


def audit_grpc_file(path: Path) -> list[str]:
    t = path.read_text(encoding="utf-8")
    issues = []
    if path.name.startswith("00"):
        if "# 1. Группы методов gRPC" not in t and "# 1. Группы" not in t:
            issues.append("00: no group table section")
        return issues
    if "# Введение" not in t.split("\n", 1)[0] and not t.startswith("# gRPC"):
        issues.append("no # Введение")
    if "# 1. Список методов" not in t:
        issues.append("no # 1. Список методов")
    sr_blocks = re.findall(r"^# SR-", t, re.M)
    for sec in ["## Общая информация", "## Логика обработки запроса", "## Статус-коды gRPC"]:
        if sr_blocks and sec not in t:
            issues.append(f"missing section: {sec}")
    anchors = re.findall(r'id="grpc-', t)
    if sr_blocks and len(anchors) < len(sr_blocks):
        issues.append(f"anchors {len(anchors)} < SR blocks {len(sr_blocks)}")
    return issues


def audit_algo_file(path: Path) -> list[str]:
    t = path.read_text(encoding="utf-8")
    if path.name.startswith("00"):
        if "# 1. Группы алгоритмов" not in t:
            issues = ["00: no # 1. Группы алгоритмов"]
        else:
            issues = []
        return issues
    issues = []
    if "# Алгоритм" not in t and "## Контекст и область применения" not in t:
        issues.append("no algorithm block template")
    return issues


def count_01_groups(svc: str) -> int:
    z = ROOT / svc / "01 - Функциональная спецификация" / "Возможности сервиса" / "00 - Общая информация.md"
    return len(re.findall(r"^### \d+\.", z.read_text(encoding="utf-8"), re.M))


def main():
    print("=== Auth etalon 04 ===")
    print(f"files: {len(list(AUTH_04.rglob('*.md')))}")
    print(f"subfolders: {sorted(p.name for p in AUTH_04.iterdir() if p.is_dir())}")

    print("\n=== Per-service 04 vs Auth layout ===")
    for svc, cfg in SERVICES.items():
        d = ROOT / svc / "04 - Бекенд, API и Контракты"
        print(f"\n--- {svc} ---")
        if not d.exists():
            print("  MISSING entire 04 folder")
            continue
        if not (d / "README.md").exists():
            print("  MISSING README.md")
        groups_01 = count_01_groups(svc)
        grpc_groups = len([f for f in (d / "Методы API/gRPC").glob("*.md") if f.name.startswith(("01", "02", "03", "04", "05", "06", "07", "08", "09"))]) if (d / "Методы API/gRPC").exists() else 0
        rest_groups = len([f for f in (d / "Методы API/REST API").glob("[0]*.md") if not f.name.startswith("00")]) if (d / "Методы API/REST API").exists() else 0
        dto_files = len(list((d / "Методы API/DTO").glob("*.md"))) if (d / "Методы API/DTO").exists() else 0
        print(f"  01 groups: {groups_01} | gRPC group files: {grpc_groups} | REST groups: {rest_groups} | DTO files: {dto_files}")

        for sub in AUTH_SUBFOLDERS:
            p = d / sub
            if sub in cfg.get("skip", []):
                if p.exists():
                    print(f"  WARN should not use: {sub} (exists)")
            elif sub in cfg.get("expect", []):
                if not p.exists():
                    print(f"  MISSING expected: {sub}")
                else:
                    n = len(list(p.glob("*.md")))
                    opt = " (optional)" if sub in cfg.get("optional", []) else ""
                    print(f"  OK {sub}: {n} md{opt}")

        # gRPC template check
        grpc = d / "Методы API/gRPC"
        if grpc.exists():
            bad = 0
            for f in grpc.glob("*.md"):
                iss = audit_grpc_file(f)
                if iss:
                    print(f"  gRPC template {f.name}: {iss}")
                    bad += 1
            if bad == 0:
                print("  gRPC template: all files pass")

        algo = d / "Алгоритмы и методы бекенда"
        if algo.exists():
            bad = 0
            for f in algo.glob("*.md"):
                iss = audit_algo_file(f)
                if iss:
                    print(f"  algo template {f.name}: {iss}")
                    bad += 1

    # duplicates in Billing integrations
    bill_int = ROOT / "Billing Service" / "04 - Бекенд, API и Контракты" / "Интеграции со сторонними сервисами"
    if bill_int.exists():
        names = [f.name for f in bill_int.glob("*.md")]
        if len(names) != len(set(names)):
            print("\nBilling integrations DUPLICATE filenames")


if __name__ == "__main__":
    main()
