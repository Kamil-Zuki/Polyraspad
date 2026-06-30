# -*- coding: utf-8 -*-
"""Audit folder 04 completeness vs 01 groups per service."""
import re
import pathlib

root = pathlib.Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")
skip = {"(Done) Authorization Service", "Шаблон документации микросервиса STEOS", "05 - Сводная документация"}

SUBFOLDERS = [
    "Методы API/gRPC",
    "Методы API/DTO",
    "Методы API/REST API",
    "Методы API/Socket",
    "Интеграции со сторонними сервисами",
    "Работа с Rabbit MQ",
    "Работа с Redis",
    "Алгоритмы и методы бекенда",
]

# Expected layers per service (from architecture)
EXPECTED = {
    "Aggregator Service": {"gRPC": False, "REST": True, "DTO": True, "Integrations": True, "Algorithms": True},
    "Agent Service": {"gRPC": True, "DTO": True, "REST": False, "Integrations": True, "Algorithms": True},
    "Authorization Module": {"gRPC": True, "DTO": True, "REST": True, "Integrations": True, "Algorithms": True},
    "Billing Service": {"gRPC": True, "DTO": False, "REST": False, "Integrations": True, "Algorithms": True},
    "Media Service": {"gRPC": True, "DTO": True, "REST": False, "Integrations": True, "Algorithms": True},
}

def count_group_files(folder):
    if not folder.exists():
        return 0
    return sum(1 for f in folder.glob("*.md") if f.name.startswith("00") is False and re.match(r"^\d{2}", f.name))

def has_00(folder):
    if not folder.exists():
        return False
    return any(f.name.startswith("00") for f in folder.glob("*.md"))

print("=== FOLDER 04 AUDIT ===\n")
for svc, exp in sorted(EXPECTED.items()):
    base = root / svc / "04 - Бекенд, API и Контракты"
    print(f"## {svc}")
    if not base.exists():
        print("  MISSING entire 04 folder\n")
        continue
    readme = (base / "README.md").exists()
    print(f"  README: {'yes' if readme else 'NO'}")
    gaps = []
    for key, needed in exp.items():
        if not needed:
            continue
        if key == "gRPC":
            p = base / "Методы API" / "gRPC"
            n = count_group_files(p)
            ok = has_00(p) and n >= 1
            proto = list(p.glob("*.proto")) if p.exists() else []
            print(f"  gRPC: 00={has_00(p)} groups={n} proto={len(proto)}")
            if not ok:
                gaps.append("gRPC incomplete")
        elif key == "REST":
            p = base / "Методы API" / "REST API"
            n = count_group_files(p)
            # Aggregator has 16 groups in 01
            need = 16 if svc == "Aggregator Service" else 1
            ok = has_00(p) and n >= need * 0.5  # at least half for partial
            print(f"  REST: 00={has_00(p)} groups={n} (need ~{need})")
            if n < need:
                gaps.append(f"REST {n}/{need}")
        elif key == "DTO":
            p = base / "Методы API" / "DTO"
            n = count_group_files(p)
            need = 16 if svc == "Aggregator Service" else (4 if svc == "Media Service" else (1 if svc == "Agent Service" else 1))
            print(f"  DTO: 00={has_00(p)} groups={n}")
            if svc == "Aggregator Service" and n < 10:
                gaps.append(f"DTO {n}/16")
            elif svc == "Agent Service" and n < 1:
                gaps.append("DTO missing groups")
        elif key == "Integrations":
            p = base / "Интеграции со сторонними сервисами"
            n = count_group_files(p)
            print(f"  Integrations: 00={has_00(p)} files={n}")
            if not has_00(p) or n < 1:
                gaps.append("Integrations incomplete")
        elif key == "Algorithms":
            p = base / "Алгоритмы и методы бекенда"
            n = count_group_files(p)
            print(f"  Algorithms: 00={has_00(p)} files={n}")
            if not has_00(p) or n < 1:
                gaps.append("Algorithms incomplete")
    status = "OK" if not gaps else "GAPS: " + "; ".join(gaps)
    print(f"  => {status}\n")

# Count 01 groups vs group files
print("=== FOLDER 01 COVERAGE ===\n")
for svc_dir in sorted(root.iterdir()):
    if not svc_dir.is_dir() or svc_dir.name in skip:
        continue
    cap = svc_dir / "01 - Функциональная спецификация" / "Возможности сервиса"
    if not cap.exists():
        continue
    zero = cap / "00 - Общая информация.md"
    if not zero.exists():
        continue
    groups = [f for f in cap.glob("*.md") if f.name != "00 - Общая информация.md" and re.match(r"^\d{2}", f.name)]
    text = zero.read_text(encoding="utf-8")
    sr_count = len(re.findall(r"\*\*(SR-[A-Z0-9_-]+)\*\*", text))
    print(f"{svc_dir.name}: groups={len(groups)} SR_in_00={sr_count}")
