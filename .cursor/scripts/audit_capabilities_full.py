# -*- coding: utf-8 -*-
"""Аудит полноты «Описание возможностей» / SR в 01 Возможности сервиса."""
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


def score_hl(hl: str) -> list[str]:
    issues = []
    text = re.sub(r"\s+", " ", hl.strip())
    if len(text) < 150:
        issues.append("hl_short")
    if "Представим" not in hl and "Представьте" not in hl:
        issues.append("hl_no_metaphor")
    if len(re.findall(r"^\d+\.\s", hl, re.M)) < 2:
        issues.append("hl_few_steps")
    return issues


def audit_service(svc: str) -> dict:
    cap = ROOT / svc / "01 - Функциональная спецификация" / "Возможности сервиса"
    zero = cap / "00 - Общая информация.md"
    result = {
        "svc": svc,
        "errors": [],
        "sr_00": set(),
        "sr_groups": set(),
        "groups_00": [],
        "group_files": 0,
        "incomplete_sr": [],
        "struct_missing": [],
    }
    if not zero.exists():
        result["errors"].append("missing 00")
        return result

    t0 = zero.read_text(encoding="utf-8")
    result["sr_00"] = set(re.findall(r"\*\*(SR-[A-Z0-9-]+)\*\*", t0))
    result["groups_00"] = re.findall(r"^### (\d+)\.\s+(.+)$", t0, re.M)

    group_files = [f for f in cap.glob("*.md") if not f.name.startswith("00")]
    result["group_files"] = len(group_files)

    for f in group_files:
        t = f.read_text(encoding="utf-8")
        cap_sr = set(
            re.findall(
                r"\*\*(SR-[A-Z0-9-]+)\*\*",
                t.split("# Детальная спецификация требований")[0],
            )
        )
        detail_sr = set(re.findall(r"^## (SR-[A-Z0-9-]+):", t, re.M))
        for sid in cap_sr - detail_sr:
            result["struct_missing"].append(f"{sid} in {f.name}: cap without detail")
        for sid in detail_sr - cap_sr:
            result["struct_missing"].append(f"{sid} in {f.name}: detail without cap")

        for part in re.split(r"(?=^## SR-)", t, flags=re.M)[1:]:
            hdr = part.split("\n", 1)[0]
            sid = re.search(r"(SR-[A-Z0-9-]+)", hdr)
            if not sid:
                continue
            sid = sid.group(1)
            result["sr_groups"].add(sid)
            issues = []
            if "### 1. Цель и ключевые принципы" not in part:
                issues.append("no_principles")
            if "### 2. Высокоуровневое описание" not in part:
                issues.append("no_hl")
            elif "### 3. Примеры взаимодействия" not in part:
                issues.append("no_scenarios")
            else:
                m = re.search(
                    r"### 2\. Высокоуровневое описание\s*\n(.*?)(?=\n### 3\.|\Z)",
                    part,
                    re.S,
                )
                if m:
                    issues.extend(score_hl(m.group(1)))
                # scenario depth: at least one #### Сценарий
                scen = part.split("### 3. Примеры взаимодействия", 1)
                if len(scen) > 1 and "#### Сценарий" not in scen[1]:
                    issues.append("no_scenario_heading")
            if issues:
                result["incomplete_sr"].append((sid, f.name, issues))

    only_00 = sorted(result["sr_00"] - result["sr_groups"])
    only_grp = sorted(result["sr_groups"] - result["sr_00"])
    if only_00:
        result["errors"].append(f"SR in 00 not in groups: {only_00}")
    if only_grp:
        result["errors"].append(f"SR in groups not in 00: {only_grp}")
    if result["group_files"] != len(result["groups_00"]):
        result["errors"].append(
            f"group files {result['group_files']} != 00 sections {len(result['groups_00'])}"
        )

    return result


def main():
    total_sr = 0
    complete_sr = 0
    for svc in SERVICES:
        r = audit_service(svc)
        incomplete = len(r["incomplete_sr"])
        sr_count = len(r["sr_groups"])
        total_sr += sr_count
        complete_sr += sr_count - incomplete
        print(f"\n=== {r['svc']} ===")
        print(
            f"  groups: {len(r['groups_00'])} in 00, {r['group_files']} files | "
            f"SR: {len(r['sr_00'])} in 00, {sr_count} in groups"
        )
        for e in r["errors"]:
            print(f"  ERROR: {e}")
        for sm in r["struct_missing"][:5]:
            print(f"  STRUCT: {sm}")
        if incomplete:
            print(f"  INCOMPLETE SR blocks: {incomplete}/{sr_count}")
            for sid, fn, iss in r["incomplete_sr"][:8]:
                print(f"    {sid} ({fn}): {','.join(iss)}")
            if incomplete > 8:
                print(f"    ... +{incomplete - 8} more")

    print(f"\n=== TOTAL ===")
    print(f"SR blocks structurally present: {total_sr}")
    print(f"SR blocks FULL (etalon HL+scenarios): {complete_sr}/{total_sr}")
    print(f"INCOMPLETE: {total_sr - complete_sr}")


if __name__ == "__main__":
    main()
