import pathlib
root = pathlib.Path(r"c:\Users\Zuko\Desktop\01Projects\Development_Documents\Polyraspad\Docs")
skip = {"(Done) Authorization Service", "Шаблон документации микросервиса STEOS"}
folders = ["01", "02", "03", "04", "99"]
labels = {
    "01": "01 - Функциональная спецификация",
    "02": "02 - Архитектура",
    "03": "03 - Модель Данных",
    "04": "04 - Бекенд, API и Контракты",
    "99": "99 - Staging — Разрывы согласованности (DO NOT DELETE)",
}
print("SERVICE | 01 | 02 | 03 | 04 | 99")
print("---|---:|---:|---:|---:|---:")
for svc in sorted(root.iterdir()):
    if not svc.is_dir() or svc.name in skip:
        continue
    row = [svc.name]
    for k in folders:
        p = svc / labels[k]
        row.append(str(len(list(p.rglob("*.md")))) if p.exists() else "—")
    print(" | ".join(row))
