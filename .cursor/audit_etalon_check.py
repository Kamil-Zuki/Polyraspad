import re
from pathlib import Path

docs = Path(__file__).resolve().parents[1] / "Docs"
skip = {"Шаблон документации микросервиса STEOS", "(Done) Authorization Service"}


def audit_01_group(fpath):
    t = fpath.read_text(encoding="utf-8")
    issues = []
    if not re.search(r"^# Группа \d+:", t, re.M):
        issues.append("no_Gruppa")
    if "**Метафора:**" not in t:
        issues.append("no_Metafora")
    if "# Детальная спецификация требований" not in t:
        issues.append("no_detailed_spec")
    srs = re.findall(r"^## (SR-[A-Z0-9-]+):", t, re.M)
    for sr in srs:
        m = re.search(rf"^## {re.escape(sr)}:.*?(?=^## SR-|^## [^#]|\Z)", t, re.M | re.S)
        if not m:
            continue
        b = m.group(0)
        for sec in ("### 1.", "### 2.", "### 3."):
            if sec not in b:
                issues.append(f"{sr}:missing_{sec}")
    return issues


def audit_grpc_etalon(fpath):
    t = fpath.read_text(encoding="utf-8")
    issues = []
    if fpath.name.startswith("00"):
        return issues
    if "# Введение" not in t:
        issues.append("no_Vvedenie")
    if "# 1. Список методов" not in t:
        issues.append("no_method_table")
    rpc_h = len(re.findall(r"^# SR-", t, re.M))
    spans = len(re.findall(r"<span id=.grpc-", t))
    if rpc_h > 0 and spans < rpc_h:
        issues.append(f"anchors_{spans}_of_{rpc_h}")
    if rpc_h > 0 and "## Общая информация" not in t:
        issues.append("no_RPC_template")
    if rpc_h > 0 and "## Логика обработки" not in t:
        issues.append("no_logic_section")
    if rpc_h > 0 and "Статус-коды gRPC" not in t:
        issues.append("no_status_table")
    if "Реализация:" in t[:300] and "# Введение" not in t[:300]:
        issues.append("STUB_format")
    return issues


def audit_rest_etalon(fpath):
    t = fpath.read_text(encoding="utf-8")
    if fpath.name.startswith("00"):
        return []
    issues = []
    if "# Введение" not in t:
        issues.append("no_Vvedenie")
    if "# 1. Список" not in t:
        issues.append("no_endpoint_list")
    return issues


for svc in sorted(docs.iterdir(), key=lambda x: x.name):
    if not svc.is_dir() or svc.name in skip:
        continue
    cap = svc / "01 - Функциональная спецификация" / "Возможности сервиса"
    p04 = svc / "04 - Бекенд, API и Контракты"
    if not cap.exists():
        continue

    i01 = []
    for f in cap.glob("*.md"):
        if f.name.startswith("00"):
            continue
        i01.extend([f.name + ":" + i for i in audit_01_group(f)])

    grpc_issues = []
    rest_issues = []
    missing_04 = not p04.exists()
    if p04.exists():
        grpc_dir = p04 / "Методы API/gRPC"
        if grpc_dir.exists():
            for f in grpc_dir.glob("*.md"):
                grpc_issues.extend([f.name + ":" + i for i in audit_grpc_etalon(f)])
        rest_dir = p04 / "Методы API/REST API"
        if rest_dir.exists():
            for f in rest_dir.glob("*.md"):
                rest_issues.extend([f.name + ":" + i for i in audit_rest_etalon(f)])

    print("===", svc.name, "===")
    print("  01:", "PASS" if not i01 else f"FAIL ({len(i01)})")
    if missing_04:
        print("  04: MISSING")
    else:
        print("  04 gRPC:", "PASS" if not grpc_issues else f"FAIL ({len(grpc_issues)})")
        print("  04 REST:", "PASS" if not rest_issues else f"FAIL ({len(rest_issues)})")
    for x in i01[:3]:
        print("   01:", x)
    for x in grpc_issues[:5]:
        print("   grpc:", x)
    for x in rest_issues[:3]:
        print("   rest:", x)
    print()
