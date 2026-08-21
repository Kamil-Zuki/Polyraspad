import httpx

resp = httpx.get("https://openrouter.ai/api/v1/models", timeout=20.0)
models = resp.json().get("data", [])
free_models = [
    m for m in models 
    if ":free" in m.get("id", "") or (
        m.get("pricing", {}).get("prompt") in ("0", 0) 
        and m.get("pricing", {}).get("completion") in ("0", 0)
    )
]

print(f"Total free models: {len(free_models)}\n")
for m in sorted(free_models, key=lambda x: x["id"]):
    params = m.get("supported_parameters", [])
    has_tools = "tools" in params or "function_call" in params
    name = m.get("name", "")
    model_id = m.get("id", "")
    ctx = m.get("context_length", 0)
    print(f"- ID: {model_id}")
    print(f"  Name: {name}")
    print(f"  Context Window: {ctx:,} tokens")
    print(f"  Tools/Function Calling: {'YES' if has_tools else 'NO'}")
    print()
