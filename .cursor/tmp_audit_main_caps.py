# -*- coding: utf-8 -*-
"""Full audit: 00 groups vs group files, SR coverage, SR block completeness."""
import re
import pathlib
import json

root = pathlib.Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")
skip = {"(Done) Authorization Service", "Шаблон документации микросервиса STEOS", "05 - Сводная документация"}

SR_RE = re.compile(r"\*\*(SR-[A-Z0-9_-]+)\*\*")
GROUP_FILE_RE = re.compile(r"^\d{2}\s*-\s*.+\.md$")

def normalize_sr(code: str) -> str:
    """SR-XXX from table or ## SR-XXX: Title header."""
    code = code.strip()
    m = re.match(r"(SR-[A-Z0-9_-]+)", code)
    return m.group(1) if m else code

def extract_sr_codes(text):
    return [normalize_sr(c) for c in SR_RE.findall(text)]

def sr_blocks_in_file(text):
    """Return dict sr_code -> block metadata."""
    result = {}
    parts = re.split(r"(?=^## SR-)", text, flags=re.M)
    for part in parts[1:]:
        m = re.match(r"## (SR-[A-Z0-9_-]+)", part)
        if not m:
            continue
        sr = normalize_sr(m.group(1))
        h2 = re.search(
            r"### 2\. Высокоуровневое описание\s*\n(.*?)(?=\n### 3\.|\n## SR-|\Z)",
            part, re.S,
        )
        body = h2.group(1).strip() if h2 else ""
        result[sr] = {
            "has1": "### 1. Цель и ключевые принципы" in part,
            "has2": bool(h2),
            "has3": "### 3. Примеры взаимодействия" in part,
            "h2_len": len(body),
            "h2_metaphor": bool(re.search(r"^Представим|^Представьте", body, re.M)),
            "h2_takim": "Таким образом" in body,
        }
    return result

report = {"services": {}, "summary": {}}

for svc_dir in sorted(root.iterdir()):
    if not svc_dir.is_dir() or svc_dir.name in skip:
        continue
    cap_dir = svc_dir / "01 - Функциональная спецификация" / "Возможности сервиса"
    if not cap_dir.exists():
        continue

    svc = svc_dir.name
    zero = cap_dir / "00 - Общая информация.md"
    if not zero.exists():
        report["services"][svc] = {"error": "missing 00"}
        continue

    zero_text = zero.read_text(encoding="utf-8")
    sr_in_00 = extract_sr_codes(zero_text)

    # Count groups from table or file list
    group_files = sorted(
        f for f in cap_dir.glob("*.md")
        if f.name != "00 - Общая информация.md" and GROUP_FILE_RE.match(f.name)
    )

    sr_in_groups = []
    sr_detail = {}
    group_issues = []

    for gf in group_files:
        gt = gf.read_text(encoding="utf-8")
        srs = extract_sr_codes(gt)
        hdr_srs = [normalize_sr(h) for h in re.findall(r"^## (SR-[A-Z0-9_-]+)", gt, re.M)]
        for s in hdr_srs:
            if s not in srs:
                srs.append(s)
        sr_in_groups.extend(srs)
        blocks = sr_blocks_in_file(gt)

        # group file structure
        miss = []
        if not re.search(r"^# Группа \d+:", gt, re.M):
            miss.append("NO_GROUP_H1")
        if not re.search(r"^## Возможности данного раздела", gt, re.M):
            miss.append("NO_SR_TABLE")
        if not re.search(r"^# Детальная спецификация", gt, re.M):
            miss.append("NO_DETAIL_H1")
        if miss:
            group_issues.append((gf.name, miss))

        for sr, b in blocks.items():
            sr_detail[sr] = {**b, "file": gf.name}

    sr_in_00_set = set(sr_in_00)
    sr_in_groups_set = set(sr_in_groups)

    missing_files = []  # groups in 00 file list without file
    # parse group numbers from 00
    group_nums_00 = re.findall(r"\|\s*\*\*(\d+)\*\*", zero_text)
    group_nums_files = [int(f.name[:2]) for f in group_files]

    only_00 = sr_in_00_set - sr_in_groups_set
    only_groups = sr_in_groups_set - sr_in_00_set

    incomplete_sr = []
    thin_h2 = []
    for sr in sr_in_00_set:
        if sr not in sr_detail:
            incomplete_sr.append((sr, "NO_DETAIL_BLOCK"))
            continue
        d = sr_detail[sr]
        if not (d["has1"] and d["has2"] and d["has3"]):
            incomplete_sr.append((sr, f"missing blocks: 1={d['has1']} 2={d['has2']} 3={d['has3']}"))
        elif not d["h2_metaphor"] or d["h2_len"] < 80 or not d["h2_takim"]:
            thin_h2.append((sr, d["file"], d["h2_metaphor"], d["h2_len"], d["h2_takim"]))

    report["services"][svc] = {
        "groups_00": len(group_nums_00) or len(set(group_nums_files)),
        "group_files": len(group_files),
        "sr_in_00": len(sr_in_00_set),
        "sr_in_groups": len(sr_in_groups_set),
        "only_00": sorted(only_00),
        "only_groups": sorted(only_groups),
        "group_structure_issues": group_issues,
        "incomplete_sr": incomplete_sr,
        "thin_h2_count": len(thin_h2),
        "thin_h2": thin_h2[:5],
        "group_nums_00": sorted(set(int(x) for x in group_nums_00)) if group_nums_00 else sorted(set(group_nums_files)),
        "group_nums_files": sorted(set(group_nums_files)),
    }

# totals
total_sr = sum(r.get("sr_in_00", 0) for r in report["services"].values())
total_incomplete = sum(len(r.get("incomplete_sr", [])) for r in report["services"].values())
total_thin = sum(r.get("thin_h2_count", 0) for r in report["services"].values())
report["summary"] = {
    "services": len(report["services"]),
    "total_sr": total_sr,
    "incomplete_sr": total_incomplete,
    "thin_h2": total_thin,
}

out = root.parent / ".cursor" / "audit_main_capabilities.json"
out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

# print summary
print("=== MAIN CAPABILITIES AUDIT ===")
for svc, r in sorted(report["services"].items()):
    if "error" in r:
        print(f"{svc}: ERROR {r['error']}")
        continue
    ok = not r["only_00"] and not r["only_groups"] and not r["incomplete_sr"] and not r["group_structure_issues"]
    status = "OK" if ok and r["thin_h2_count"] == 0 else "GAPS"
    print(f"\n{svc} [{status}]")
    print(f"  groups: 00={r['groups_00']} files={r['group_files']} nums={r['group_nums_00']} vs files={r['group_nums_files']}")
    print(f"  SR: 00={r['sr_in_00']} groups={r['sr_in_groups']} incomplete={len(r['incomplete_sr'])} thin_h2={r['thin_h2_count']}")
    if r["only_00"]:
        print(f"  SR only in 00: {r['only_00']}")
    if r["only_groups"]:
        print(f"  SR only in groups: {r['only_groups']}")
    if r["incomplete_sr"]:
        for x in r["incomplete_sr"][:5]:
            print(f"  incomplete: {x}")
    if r["group_structure_issues"]:
        print(f"  structure: {r['group_structure_issues']}")

print(f"\nTOTAL: {total_sr} SR, {total_incomplete} incomplete blocks, {total_thin} thin ###2")
print(f"JSON: {out}")
