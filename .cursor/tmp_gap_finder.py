# -*- coding: utf-8 -*-
"""Find documentation gaps: REST detail blocks, thin sections."""
import re
from pathlib import Path

ROOT = Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")

def rest_gaps():
    rest = ROOT / "Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API"
    print("=== Aggregator REST: table rows vs detail blocks ===")
    for f in sorted(rest.glob("*.md")):
        if f.name.startswith("00"):
            continue
        text = f.read_text(encoding="utf-8")
        table_routes = len(re.findall(r"\|\s*(GET|POST|PUT|DELETE|PATCH)\s*\|\s*`(/api[^`]+)`", text))
        details = len(re.findall(r"^# SR-AGG-", text, re.M))
        if table_routes > details:
            print(f"GAP {f.name}: table={table_routes} details={details} missing~{table_routes-details}")

def auth_mod_dup():
    grpc = ROOT / "Authorization Module/04 - Бекенд, API и Контракты/Методы API/gRPC"
    print("\n=== Auth Module gRPC files ===")
    for f in sorted(grpc.glob("*.md")):
        anchors = len(re.findall(r'id="grpc-', f.read_text(encoding="utf-8")))
        print(f"  {f.name}: anchors={anchors}")

def agg_algo_vs_01():
    caps = ROOT / "Aggregator Service/01 - Функциональная спецификация/Возможности сервиса"
    algo = ROOT / "Aggregator Service/04 - Бекенд, API и Контракты/Алгоритмы и методы бекенда"
    sr_01 = set()
    for f in caps.glob("*.md"):
        if f.name.startswith("00"):
            continue
        text = f.read_text(encoding="utf-8")
        sr_01.update(re.findall(r"SR-AGG-[A-Z0-9-]+", text))
    algo_text = ""
    for f in algo.rglob("*.md"):
        algo_text += f.read_text(encoding="utf-8")
    missing_algo = [s for s in sorted(sr_01) if s not in algo_text and "OPS" not in s]
    print(f"\n=== Aggregator SR in 01 not mentioned in algorithms ({len(missing_algo)}) ===")
    for s in missing_algo[:30]:
        print(f"  {s}")
    if len(missing_algo) > 30:
        print(f"  ... +{len(missing_algo)-30}")

rest_gaps()
auth_mod_dup()
agg_algo_vs_01()
