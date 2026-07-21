# Project Skills — microservice documentation

Вызов: `npx openskills read <skill-name>`

| Skill | Когда использовать |
| :--- | :--- |
| `docs-04-coordinator` | Заполнить или спланировать весь `04` для сервиса — manifest, порядок, делегирование |
| `docs-04-write` | Писать/дописывать файлы `04` по manifest (block templates — в `.cursor/rules/`) |
| `docs-04-verify` | Readonly-аудит `04` vs `01`/`03`/`02`, ISSUE в `99 - Staging` |

**Rules** (G0–G3) — автоматически при редактировании файлов. **Skills** — playbook для пакетных прогонов.

Subagents: `.cursor/agents/docs-04-*.md` — обёртки с фиксированной ролью (`@docs-04-coordinator` и т.д.).
