# -*- coding: utf-8 -*-
"""Code vs Docs audit for STEOS 04 folders."""
import re
from pathlib import Path

ROOT = Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad")

# --- gRPC from source protos ---
GRPC_SOURCES = {
    "Agent Service": ROOT / "AgentService/Protos/agent.proto",
    "Billing Service": ROOT / "BillingService/Protos/billing.proto",
    "Media Service": ROOT / "MediaService/Protos/media.proto",
    "Authorization Module": ROOT / "authorization-module/authorization-module.API/Protos/authorization.proto",
}

DOCS_GRPC = {
    "Agent Service": ROOT / "Docs/Agent Service/04 - Бекенд, API и Контракты/Методы API/gRPC",
    "Billing Service": ROOT / "Docs/Billing Service/04 - Бекенд, API и Контракты/Методы API/gRPC",
    "Media Service": ROOT / "Docs/Media Service/04 - Бекенд, API и Контракты/Методы API/gRPC",
    "Authorization Module": ROOT / "Docs/Authorization Module/04 - Бекенд, API и Контракты/Методы API/gRPC",
}


def extract_rpcs(proto_path: Path) -> list[str]:
    if not proto_path.exists():
        return []
    text = proto_path.read_text(encoding="utf-8")
    return re.findall(r"^\s*rpc\s+(\w+)\s+", text, re.M)


def doc_grpc_anchors(docs_grpc_dir: Path) -> set[str]:
    if not docs_grpc_dir.exists():
        return set()
    anchors = set()
    for f in docs_grpc_dir.rglob("*.md"):
        text = f.read_text(encoding="utf-8")
        anchors.update(re.findall(r'id="grpc-(\w+)"', text))
    return anchors


print("=" * 60)
print("gRPC: code vs #grpc-* anchors in Docs/04")
print("=" * 60)
grpc_gaps_all = []
for svc, proto in GRPC_SOURCES.items():
    code_rpcs = extract_rpcs(proto)
    anchors = doc_grpc_anchors(DOCS_GRPC[svc])
    missing = [r for r in code_rpcs if r not in anchors]
    extra = [a for a in anchors if a not in code_rpcs]
    status = "OK" if not missing else "GAP"
    print(f"\n{svc} [{status}] code={len(code_rpcs)} anchors={len(anchors)}")
    if missing:
        print(f"  MISSING anchors: {missing}")
        grpc_gaps_all.extend((svc, m) for m in missing)
    if extra:
        print(f"  EXTRA anchors (not in proto): {extra}")

# --- Aggregator REST ---
AGG = ROOT / "AggregatorService/Controllers"
REST_DOCS = ROOT / "Docs/Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API"

route_re = re.compile(
    r'\[Route\("([^"]+)"\)\]|'
    r'\[Http(?:Get|Post|Put|Delete|Patch)\("([^"]*)"\)\]|'
    r'\[Http(?:Get|Post|Put|Delete|Patch)\]'
)


def extract_rest_routes() -> list[tuple[str, str, str]]:
    routes = []
    for ctrl in sorted(AGG.glob("*Controller.cs")):
        text = ctrl.read_text(encoding="utf-8")
        base = ""
        m = re.search(r'\[Route\("api/([^"]+)"\)\]', text)
        if m:
            base = "/api/" + m.group(1).rstrip("/")
        elif re.search(r'\[Route\("api"\)\]', text):
            base = "/api"
        else:
            continue
        for hm in re.finditer(
            r'\[(Http(?:Get|Post|Put|Delete|Patch))(?:\("([^"]*)"\))?\]',
            text,
        ):
            method = hm.group(1).replace("Http", "").upper()
            sub = hm.group(2) or ""
            if sub.startswith("/"):
                sub = sub[1:]
            path = base
            if sub:
                path = f"{base}/{sub}" if not base.endswith("/") else f"{base}{sub}"
            path = re.sub(r"/+", "/", path)
            routes.append((ctrl.stem, method, path))
    return routes


def doc_rest_paths() -> set[str]:
    paths = set()
    if not REST_DOCS.exists():
        return paths
    for f in REST_DOCS.glob("*.md"):
        text = f.read_text(encoding="utf-8")
        for m in re.finditer(r"`(GET|POST|PUT|DELETE|PATCH)\s+\|?\s*`([^`]+)`", text):
            paths.add(m.group(2).strip())
        for m in re.finditer(r"\|\s*(GET|POST|PUT|DELETE|PATCH)\s*\|\s*`([^`]+)`", text):
            paths.add(m.group(2).strip())
        for m in re.finditer(r"`(GET|POST|PUT|DELETE|PATCH)\s+([^`|]+)`", text):
            p = m.group(2).strip()
            if p.startswith("/api"):
                paths.add(p.split()[0])
    return paths


code_routes = extract_rest_routes()
doc_paths = doc_rest_paths()

print("\n" + "=" * 60)
print(f"Aggregator REST: {len(code_routes)} endpoints in code")
print("=" * 60)

# Normalize for comparison
def norm(p: str) -> str:
    p = p.lower().replace("{id}", "{id}").replace("{deckid}", "{deckid}")
    p = re.sub(r"\{[^}]+\}", "{}", p)
    return p.rstrip("/")


code_norm = {norm(r[2]): r for r in code_routes}
doc_norm = {norm(p) for p in doc_paths}

missing_rest = []
for n, (ctrl, method, path) in code_norm.items():
    if n not in doc_norm:
        # fuzzy: check if path substring in any doc file
        found = any(n.replace("/{}", "") in dp.replace("/{}", "") for dp in doc_norm)
        if not found:
            missing_rest.append((ctrl, method, path))

if missing_rest:
    print(f"\nPotentially UNDOCUMENTED routes ({len(missing_rest)}):")
    for ctrl, method, path in sorted(missing_rest, key=lambda x: x[2])[:40]:
        print(f"  {method:6} {path}  ({ctrl})")
    if len(missing_rest) > 40:
        print(f"  ... and {len(missing_rest)-40} more")
else:
    print("\nAll code routes appear referenced in REST docs (normalized).")

# Print code routes for manual review - group by controller
print("\n--- Code routes by controller ---")
from collections import defaultdict
by_ctrl = defaultdict(list)
for ctrl, method, path in code_routes:
    by_ctrl[ctrl].append(f"{method:6} {path}")
for ctrl in sorted(by_ctrl):
    print(f"\n{ctrl}:")
    for r in by_ctrl[ctrl]:
        print(f"  {r}")
