# Git Branching Workflow для Polyraspad

> **Статус:** действующий процесс на этапе финализации MVP и подготовки к деплою на VPS.
> **Цель:** `master` всегда остаётся рабочим и готовым к деплою. Любые изменения проходят через ветки и проверки.

---

## 1. Главное правило

- **`master`** — единственная production-ветка. В ней должен быть код, который можно задеплоить.
- Все фичи, правки, интеграции и эксперименты делаются в **отдельных ветках**.
- Перед merge обязательны: сборка, тесты и быстрая ревью (хотя бы одна пара глаз или второй агент).

---

## 2. Типы веток

| Префикс | Когда использовать | Пример |
|---------|-------------------|--------|
| `feature/` | Новая функциональность | `feature/yookassa-payments` |
| `fix/` | Исправление бага | `fix/deck-tree-stats-mapping` |
| `docs/` | Только документация | `docs/editor-ux-redesign` |
| `release/` | Подготовка к релизу/деплою | `release/v0.9.0-mvp` |
| `hotfix/` | Срочный фикс на production | `hotfix/auth-500-error` |

---

## 3. Работа с root-репозиторием (включая `Docs/`)

`Docs/` находится в root-репозитории, а не в submodule.

```powershell
# 1. Обновить master
git checkout master
git pull --rebase

# 2. Создать ветку
git checkout -b docs/editor-ux-redesign

# 3. Редактировать файлы в Docs/
# ...

# 4. Проверить, что ничего лишнего не попало
git status

# 5. Закоммитить и запушить
git add Docs/...
git commit -m "docs: editor UX redesign workflow"
git push -u origin docs/editor-ux-redesign

# 6. После review/тестов — merge в master
git checkout master
git pull --rebase
git merge --no-ff docs/editor-ux-redesign
git push
```

### Важно для `Docs/`

- Если изменения в `Docs/` относятся к конкретному сервису (например, обновление API Decks), старайтесь делать их в одноимённой root-ветке рядом с кодом сервиса (например, `feature/deck-tree-docs-and-stats`).
- Документация микросервисов (`Docs/<Service>/`) управляется правилами `Docs/.cursor/rules/`. Не редактируйте `03` (модель данных) без явного запроса.

---

## 4. Работа с submodules — критически важно

Шесть основных компонентов — submodules:

- `AggregatorService`
- `VocabularyService`
- `AgentService`
- `BillingService`
- `authorization-module`
- `polyraspad-frontend`
- `inclusive`

**Главная опасность:** открыть папку submodule, отредактировать файлы и закоммитить в **detached HEAD**. Тогда коммит "повиснет" — его не будет в `master` submodule, и root не сможет корректно обновить ссылку.

### 4.1 Проверка перед редактированием submodule

Зайдите в папку submodule и убедитесь, что вы на ветке:

```powershell
cd AggregatorService
git branch --show-current
```

- Если вывод пустой (`* (HEAD detached at ...)`) — **не коммитьте**. Сначала перейдите на ветку:

```powershell
# Вариант А: продолжить работу на master (только для мелких hotfix'ов)
git checkout master
git pull --rebase

# Вариант Б: создать feature-ветку (предпочтительно)
git checkout -b feature/deck-tree-stats-mapping
```

### 4.2 Полный цикл изменения в submodule

```powershell
# 1. Войти в submodule и создать ветку
cd AggregatorService
git checkout master
git pull --rebase
git checkout -b feature/deck-tree-stats-mapping

# 2. Редактировать, собрать, протестировать
# dotnet build -c Release --no-restore
# dotnet test ...

# 3. Закоммитить и запушить ветку
git add -A
git commit -m "fix: add DeckDetailStats -> DeckDetailStatsDto automapper mapping"
git push -u origin feature/deck-tree-stats-mapping

# 4. Вернуться в root и обновить ссылку submodule
cd ..
git status                    # должен показать изменение AggregatorService
git add AggregatorService
git commit -m "chore: update AggregatorService submodule for deck stats fix"
git push
```

### 4.3 Как submodule оказывается в detached HEAD

Это происходит после команд вроде:

```powershell
git submodule update --init
git submodule update --remote
git checkout <commit>
```

По умолчанию submodule проверяется на конкретный коммит, а не на ветку. **Перед правкой всегда делайте `git checkout master` (или feature-ветку) внутри submodule.**

---

## 5. Чек-лист перед merge

Для каждого компонента, который затронут:

- [ ] Все submodules находятся на ветках, а не в `detached HEAD`.
- [ ] Все submodules запушены на `origin`.
- [ ] Ссылки на submodules в root-репозитории обновлены и запушены.
- [ ] Сборка проходит (`npm run build`, `dotnet build -c Release --no-restore`).
- [ ] Тесты проходят (`npm test -- --run`, `dotnet test ...`).
- [ ] В `Docs/` нет черновиков/незаконченных файлов.
- [ ] Commit message описывает, **что** и **зачем**.

---

## 6. Release / Deploy

На финишной прямой перед VPS-деплоем используйте release-ветки.

```powershell
# 1. Создать release-ветку от master
git checkout master
git pull --rebase
git checkout -b release/v0.9.0-mvp

# 2. Влить в неё готовые feature-ветки
git merge --no-ff feature/yookassa-payments
git merge --no-ff fix/deck-tree-stats-mapping

# 3. Прогнать полный набор тестов и сборку
# docker compose build
# dotnet test ...
# npm test -- --run

# 4. Задеплоить с release-ветки на VPS
# (по workflow deploy.yml или вручную)

# 5. После успешного деплоя — merge release в master
git checkout master
git merge --no-ff release/v0.9.0-mvp
git tag v0.9.0
git push --follow-tags
```

---

## 7. Интеграция ЮКасса

ЮКасса — отдельная feature-ветка, пока не протестирована и не принята — не в `master`.

```powershell
git checkout -b feature/yookassa-payments
# ... реализация webhook, provider, entitlement checks ...
# ... тесты в BillingService.Tests ...
# ... ручное тестирование через sandbox YooKassa ...
```

- Не мержить в `master`, пока не пройдены тестовые платежи.
- Перед деплоем ЮКассы убедиться, что `BILLING_DEFAULT_PROVIDER=yookassa` и `YOOKASSA_*` ключи заполнены в `.env` на VPS.

---

## 8. Казахстан + Stripe (после 300 000 ₽)

Когда появится доход и решение о переезде/зеркале:

1. Создать отдельную ветку для зеркала:
   ```powershell
   git checkout -b feature/kazakhstan-stripe-mirror
   ```
2. В ней настроить:
   - отдельный инстанс frontend/app под kz-домен;
   - Stripe provider в `BillingService`;
   - конфигурацию деплоя для второго VPS/региона.
3. Российскую ветку (`master`) не ломать — она продолжает работать на ЮКассе.
4. После стабилизации можно либо держать два долгоживущих бранча (`master` для РФ, `master-kz` для Казахстана), либо выделить конфигурацию провайдера в переменные окружения и держать один код.

---

## 9. Что делать, если всё же закоммитили в detached HEAD

```powershell
cd <submodule>

# 1. Запомнить текущий commit
git log --oneline -1

# 2. Создать ветку из этого commit
git checkout -b feature/recovered-work

# 3. Запушить
git push -u origin feature/recovered-work

# 4. Вернуться в root и обновить ссылку
cd ..
git add <submodule>
git commit -m "chore: recover submodule work from detached HEAD"
git push
```

---

## 10. Короткая памятка для агентов

Перед тем как начать писать код:

1. В root: `git checkout master && git pull --rebase`
2. Создать ветку: `git checkout -b feature/...`
3. Для каждого submodule, который будете менять:
   - `cd <submodule>`
   - `git branch --show-current` → должна быть ветка
   - если пусто: `git checkout master && git pull --rebase && git checkout -b feature/...`
4. После работы: коммит + push submodule, затем коммит + push root.

---

## Связанные файлы

- `AGENTS.md` — общие правила работы с репозиторием.
- `Docs/.cursor/rules/docs-core.mdc` — правила документирования сервисов.
- `.gitmodules` — список submodule и их ветки.
