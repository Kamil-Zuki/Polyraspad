# -*- coding: utf-8 -*-
"""Compare Docs vs Auth etalon: structure, 01 SR blocks, 04 gRPC anchors."""
import re
import pathlib

root = pathlib.Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")
skip = {"(Done) Authorization Service", "Шаблон документации микросервиса STEOS", "05 - Сводная документация", ".cursor", ".obsidian"}

SERVICES = [
    "Agent Service", "Aggregator Service", "Authorization Module",
    "Billing Service", "Media Service",
]

issues = []

# --- 01 audit ---
for svc in SERVICES:
    cap = root / svc / "01 - Функциональная спецификация" / "Возможности сервиса"
    if not cap.exists():
        issues.append((svc, "01", "MISSING", "Возможности сервиса folder"))
        continue
    zero = cap / "00 - Общая информация.md"
    if zero.exists():
        t = zero.read_text(encoding="utf-8")
        for req in ["## Введение", "# Группы возможностей", "## Описание возможностей", "## Файлы групп"]:
            if req not in t:
                issues.append((svc, "01/00", "STRUCT", f"missing {req}"))
    for gf in cap.glob("*.md"):
        if gf.name == "00 - Общая информация.md":
            continue
        t = gf.read_text(encoding="utf-8")
        for req in ["# Группа", "## Возможности данного раздела", "# Детальная спецификация"]:
            if req not in t:
                issues.append((svc, "01", "STRUCT", f"{gf.name}: missing {req}"))
        parts = re.split(r"(?=^## SR-)", t, flags=re.M)
        for part in parts[1:]:
            m = re.match(r"## (SR-[A-Z0-9_-]+)", part)
            if not m:
                continue
            sr = m.group(1)
            for sec in ["### 1. Цель и ключевые принципы", "### 2. Высокоуровневое описание", "### 3. Примеры взаимодействия"]:
                if sec not in part:
                    issues.append((svc, "01", "SR", f"{gf.name} {sr}: missing {sec}"))
            h2 = re.search(r"### 2\. Высокоуровневое описание\s*\n(.*?)(?=\n### 3\.|\Z)", part, re.S)
            if h2:
                body = h2.group(1)
                if not re.search(r"^Представим|^Представьте", body, re.M):
                    issues.append((svc, "01", "H2", f"{sr}: no metaphor opener"))
                if "Таким образом" not in body:
                    issues.append((svc, "01", "H2", f"{sr}: no Таким образом"))

# --- 04 audit ---
AUTH_GRPC = root / "(Done) Authorization Service" / "04 - Бекенд, API и Контракты" / "Методы API" / "gRPC" / "00 - gRPC - Общая информация.md"
auth_grpc_has_groups = "# 1. Группы методов gRPC" if AUTH_GRPC.exists() else None

for svc in SERVICES:
    base = root / svc / "04 - Бекенд, API и Контракты"
    if not base.exists():
        issues.append((svc, "04", "MISSING", "entire folder 04"))
        continue
    if not (base / "README.md").exists():
        issues.append((svc, "04", "STRUCT", "no README.md"))

    # gRPC services
    if svc in ("Agent Service", "Billing Service", "Media Service", "Authorization Module"):
        grpc = base / "Методы API" / "gRPC"
        if not grpc.exists():
            issues.append((svc, "04", "gRPC", "missing gRPC folder"))
        else:
            g00 = grpc / "00 - gRPC - Общая информация.md"
            if not g00.exists():
                issues.append((svc, "04", "gRPC", "missing 00"))
            else:
                t = g00.read_text(encoding="utf-8")
                if "# Введение" not in t and "# Введение" not in t.replace("##", "#"):
                    if not t.strip().startswith("#"):
                        issues.append((svc, "04/gRPC/00", "STRUCT", "no # Введение"))
                if "Группы методов gRPC" not in t and "группы методов" not in t.lower():
                    issues.append((svc, "04/gRPC/00", "STRUCT", "no groups table"))
            protos = list(grpc.glob("*.proto"))
            if svc in ("Billing Service", "Media Service") and not protos:
                issues.append((svc, "04", "gRPC", "missing .proto in Docs"))
            # count grpc anchors in group files
            anchors = 0
            for f in grpc.glob("*.md"):
                if f.name.startswith("00"):
                    continue
                text = f.read_text(encoding="utf-8")
                anchors += len(re.findall(r'span id="grpc-', text))
                if "# 1. Список методов" not in text and "# 1. Список" not in text:
                    issues.append((svc, "04", f.name, "missing # 1. Список методов"))
                if "## Общая информация" not in text and anchors == 0:
                    pass  # ops-only file
            if svc == "Billing Service" and anchors < 9:
                issues.append((svc, "04", "gRPC", f"anchors {anchors}/9 expected"))
            if svc == "Media Service" and anchors < 15:
                issues.append((svc, "04", "gRPC", f"anchors {anchors}/15 expected"))

    if svc == "Aggregator Service":
        rest = base / "Методы API" / "REST API"
        dto = base / "Методы API" / "DTO"
        rest_n = sum(1 for f in rest.glob("*.md") if f.name.startswith("00") is False) if rest.exists() else 0
        dto_n = sum(1 for f in dto.glob("*.md") if f.name.startswith("00") is False) if dto.exists() else 0
        if rest_n < 16:
            issues.append((svc, "04", "REST", f"{rest_n}/16 group files"))
        if dto_n < 16:
            issues.append((svc, "04", "DTO", f"{dto_n}/16 group files (Auth etalon: 1 file per 01 group)"))
        integ = base / "Интеграции со сторонними сервисами"
        algo = base / "Алгоритмы и методы бекенда"
        if integ.exists():
            in_n = sum(1 for f in integ.glob("*.md") if not f.name.startswith("00"))
            if in_n < 3:
                issues.append((svc, "04", "Integrations", f"only {in_n} detail files"))
        if algo.exists():
            al_n = sum(1 for f in algo.glob("*.md") if not f.name.startswith("00"))
            if al_n < 3:
                issues.append((svc, "04", "Algorithms", f"only {al_n} detail files"))

    if svc == "Agent Service":
        dto = base / "Методы API" / "DTO"
        dto_n = sum(1 for f in dto.glob("*.md") if not f.name.startswith("00")) if dto.exists() else 0
        if dto_n < 1:
            issues.append((svc, "04", "DTO", "no group DTO files (only 00)"))
        integ = base / "Интеграции со сторонними сервисами"
        if integ.exists():
            names = [f.name for f in integ.glob("*.md")]
            if not any("LLM" in n or "OpenAI" in n for n in names):
                issues.append((svc, "04", "Integrations", "missing LLM provider file"))
        algo = base / "Алгоритмы и методы бекенда"
        if algo.exists():
            al_n = sum(1 for f in algo.glob("*.md") if not f.name.startswith("00"))
            if al_n < 7:
                issues.append((svc, "04", "Algorithms", f"{al_n}/~11 group files"))

# summarize
by_svc = {}
for svc, area, kind, msg in issues:
    by_svc.setdefault(svc, []).append(f"[{area}] {msg}")

print("=== ETALON COMPLIANCE AUDIT vs Auth layout ===\n")
print(f"Total issues: {len(issues)}\n")
for svc in SERVICES:
    lst = by_svc.get(svc, [])
    status = "OK" if not lst else f"{len(lst)} issues"
    print(f"## {svc}: {status}")
    for x in lst[:12]:
        print(f"  - {x}")
    if len(lst) > 12:
        print(f"  ... +{len(lst)-12} more")
    print()

ok_01 = sum(1 for s, a, k, m in issues if a.startswith("01"))
print(f"01 issues: {ok_01}")
print(f"04 issues: {len(issues) - ok_01}")
