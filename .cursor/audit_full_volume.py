# -*- coding: utf-8 -*-
"""Полнота 04 vs эталон Auth + покрытие 01 групп."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "Docs"
SKIP = {"Шаблон документации микросервиса STEOS", "(Done) Authorization Service"}


def count_01_groups(svc_path: Path):
    cap = svc_path / "01 - Функциональная спецификация" / "Возможности сервиса"
    if not cap.exists():
        return 0, 0, []
    groups = []
    srs = 0
    for f in sorted(cap.glob("*.md")):
        if f.name.startswith("00"):
            continue
        t = f.read_text(encoding="utf-8")
        srs += len(re.findall(r"^## SR-", t, re.M))
        groups.append(f.name)
    return len(groups), srs, groups


def proto_rpcs(proto_path: Path):
    if not proto_path.exists():
        return []
    t = proto_path.read_text(encoding="utf-8")
    return re.findall(r"^\s*rpc\s+(\w+)\s*\(", t, re.M)


def audit_grpc_file(fpath: Path):
    t = fpath.read_text(encoding="utf-8")
    rpc_blocks = re.findall(r"<span id=.grpc-(\w+)>", t)
    sr_headers = re.findall(r"^# (SR-[A-Z0-9-]+):", t, re.M)
    shallow = 0
    for m in re.finditer(r"<span id=.grpc-(\w+)>", t):
        start = m.start()
        nxt = t.find("<span id=", start + 1)
        block = t[start:nxt if nxt != -1 else len(t)]
        has_info = "## Общая информация" in block
        has_logic = "## Логика обработки" in block
        has_status = "Статус-коды gRPC" in block
        if not (has_info and has_logic and has_status):
            shallow += 1
    return {
        "anchors": len(rpc_blocks),
        "sr_headers": len(sr_headers),
        "shallow_blocks": shallow,
        "has_vvedenie": "# Введение" in t,
        "has_table": "# 1. Список" in t,
        "stub_format": "Реализация:" in t[:400] and "# Введение" not in t[:400],
    }


def audit_algorithms(fpath: Path):
    t = fpath.read_text(encoding="utf-8")
    if fpath.name.startswith("00"):
        return {"skip": True}
    has_io = "Вход" in t or "Input" in t or "| Вход" in t
    has_pseudo = "Псевдокод" in t or "псевдокод" in t or "```" in t
    return {"io": has_io, "pseudo": has_pseudo}


def scan_service(svc_path: Path):
    p04 = svc_path / "04 - Бекенд, API и Контракты"
    g_count, sr_count, groups = count_01_groups(svc_path)
    result = {
        "name": svc_path.name,
        "01_groups": g_count,
        "01_srs": sr_count,
        "04_exists": p04.exists(),
        "subfolders": {},
        "grpc_group_files": 0,
        "grpc_anchors": 0,
        "grpc_shallow": 0,
        "grpc_stub_files": 0,
        "proto_rpcs": [],
        "proto_path": None,
        "algo_files": 0,
        "algo_weak": 0,
        "dto_files": 0,
        "rest_files": 0,
        "integration_files": 0,
    }
    if not p04.exists():
        return result

    for sub in [
        "Методы API/gRPC",
        "Методы API/REST API",
        "Методы API/DTO",
        "Интеграции со сторонними сервисами",
        "Алгоритмы и методы бекенда",
        "Работа с Redis",
        "Работа с Rabbit MQ",
        "Методы API/Socket",
    ]:
        d = p04 / sub
        if d.exists():
            files = [f for f in d.glob("*.md") if not f.name.startswith("00")]
            protos = list(d.glob("*.proto"))
            result["subfolders"][sub] = len(files)
            if sub.endswith("gRPC"):
                result["grpc_group_files"] = len(files)
                for f in files:
                    a = audit_grpc_file(f)
                    result["grpc_anchors"] += a["anchors"]
                    result["grpc_shallow"] += a["shallow_blocks"]
                    if a["stub_format"]:
                        result["grpc_stub_files"] += 1
                for p in protos:
                    result["proto_path"] = str(p.relative_to(ROOT))
                    result["proto_rpcs"] = proto_rpcs(p)
            if sub.endswith("REST API"):
                result["rest_files"] = len(files)
            if sub.endswith("DTO"):
                result["dto_files"] = len(files)
            if "Интеграции" in sub:
                result["integration_files"] = len(files)
            if "Алгоритмы" in sub:
                result["algo_files"] = len(files)
                for f in files:
                    al = audit_algorithms(f)
                    if not al.get("skip") and not (al["io"] and al["pseudo"]):
                        result["algo_weak"] += 1

    return result


# Auth etalon baseline
etalon = scan_service(DOCS / "(Done) Authorization Service")
print("=== ETALON Auth Service ===")
print(f"  01: {etalon['01_groups']} groups, {etalon['01_srs']} SR")
print(f"  gRPC files: {etalon['grpc_group_files']}, anchors: {etalon['grpc_anchors']}, shallow: {etalon['grpc_shallow']}")
print(f"  REST: {etalon['rest_files']}, DTO: {etalon['dto_files']}, Algo: {etalon['algo_files']}, Integrations: {etalon['integration_files']}")
print(f"  Redis: {etalon['subfolders'].get('Работа с Redis', 0)}, Rabbit: {etalon['subfolders'].get('Работа с Rabbit MQ', 0)}, Socket: {etalon['subfolders'].get('Методы API/Socket', 0)}")
print()

for svc in sorted(DOCS.iterdir(), key=lambda x: x.name):
    if not svc.is_dir() or svc.name in SKIP:
        continue
    if not (svc / "01 - Функциональная спецификация").exists():
        continue
    r = scan_service(svc)
    if not r["04_exists"]:
        print(f"=== {r['name']} === NO 04")
        print(f"  01: {r['01_groups']} groups, {r['01_srs']} SR")
        print()
        continue

    ratio_g = f"{r['grpc_group_files']}/{r['01_groups']}" if r["01_groups"] else "?"
    proto_n = len(r["proto_rpcs"])
    anchor_n = r["grpc_anchors"]
    coverage = f"{anchor_n}/{proto_n}" if proto_n else f"{anchor_n} anchors"

    verdict = "OK"
    gaps = []
    if r["grpc_stub_files"] > 0:
        gaps.append(f"stub gRPC files: {r['grpc_stub_files']}")
    if r["01_groups"] and r["grpc_group_files"] < r["01_groups"]:
        gaps.append(f"gRPC files {ratio_g} vs 01 groups")
    if proto_n and anchor_n < proto_n:
        gaps.append(f"RPC docs {anchor_n}/{proto_n}")
    if r["grpc_shallow"] > 0:
        gaps.append(f"shallow RPC blocks: {r['grpc_shallow']}")
    if r["algo_files"] and r["algo_files"] < r["01_groups"]:
        gaps.append(f"algo {r['algo_files']}/{r['01_groups']}")
    if gaps:
        verdict = "GAP"

    print(f"=== {r['name']} [{verdict}] ===")
    print(f"  01: {r['01_groups']} groups, {r['01_srs']} SR")
    print(f"  gRPC: {ratio_g} files, {coverage}, shallow={r['grpc_shallow']}, stub={r['grpc_stub_files']}")
    if r["proto_path"]:
        print(f"  proto: {r['proto_path']}")
    print(f"  REST: {r['rest_files']}, DTO: {r['dto_files']}, Algo: {r['algo_files']} (weak={r['algo_weak']}), Integrations: {r['integration_files']}")
    for k, v in sorted(r["subfolders"].items()):
        if v:
            print(f"    {k}: {v}")
    if gaps:
        print(f"  GAPS: {', '.join(gaps)}")
    print()
