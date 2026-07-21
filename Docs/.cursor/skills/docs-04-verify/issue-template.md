# ISSUE file template

Path: `<Service>/99 - Staging — Разрывы согласованности (DO NOT DELETE)/ISSUE-NNN-{slug}.md`

**Канонический стиль и структура body:** [`.cursor/rules/docs-staging-issues.mdc`](../../rules/docs-staging-issues.mdc) — язык для человека, якорь **SR-ID** / **Entity.field** / **RPC**, путь к файлу вторичен.

## Расхождение `01` ↔ `03`

Сверка и типы: [`.cursor/rules/docs-staging-0103.mdc`](../../rules/docs-staging-0103.mdc)

В поле **Тип** — только классификация:

```markdown
## Тип

Пробел | Противоречие | Нейминг
```

В **«Где проблема»** обязательны якоря: `01` → `SR-…`; `03` → Entity + поле.

Реестр: `| ISSUE-NNN | Пробел | SR-… / Entity.field | В двух словах (одна строка) | Open |`

## Прочие области (`02`, `04`)

В поле **Тип**:

```markdown
## Тип

REST↔gRPC | 03↔DTO | proto↔gRPC | Rabbit↔КАР | Redis↔КАР
```

Body — по [`docs-staging-issues.mdc`](../../rules/docs-staging-issues.mdc). В **«Где проблема»** указывай:

| Источник | Якорь |
| :--- | :--- |
| `01` | `SR-…` (если есть) |
| `03` | Entity + поле |
| `04` | `rpc` / REST path / DTO name |
| `02` | КАР / поток |

Не ограничиваться путём к markdown-файлу.
