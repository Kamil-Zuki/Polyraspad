# План: Freemium-лимиты и Paywall (RF)

**Цель:** Сделать различие Free / Pro ощутимым: при исчерпании квот пользователь получает понятный отказ и путь на оплату (`/billing` → ЮKassa), без глобального редиректа «не оплатил».

**Продуктовый контекст:** [01Feature_Map.md](../Product/01Feature_Map.md) (Entitlements / Paywall).  
**Зависимости:** checkout уже есть — [01_MVP_Completion.md](01_MVP_Completion.md) (YooKassa Done).  
**Не в скоупе:** JWT entitlement claims, feature-locks (`CanUseGrammarTutor`), лимит на library books, Chrome extension.

---

## Модель (зафиксировать)

| Принцип | Решение |
|---------|---------|
| Free — валидный план | **Нет** hard-gate в `proxy.ts` / middleware |
| Deny | Числовые entitlements: `maxProjects`, `maxCards`, `aiRequestsPerDay` |
| Сигнал клиенту | gRPC `ResourceExhausted` → HTTP **402** + стабильное тело `{ code, message, limitKey }` |
| UX | Toast/модалка «Upgrade to Pro» → `/billing` |
| Seed (уже в Billing) | Free: 3 / 500 / 10 · Pro: 50 / 10000 / 100 |

> Feature Map упоминает 402 + JWT claims — для RF берём **402 + numeric map**; JWT claims — позже.

---

## As-is (коротко)

| Слой | Есть | Дыра |
|------|------|------|
| BillingService | Seed Free/Pro, `GetEntitlements`, `CheckAccess` | — |
| Vocabulary `BillingLimitService` | `CanCreateProject/Card`, `CanUseAi` | Capture / BulkCreate не вызывают `CanCreateCard` |
| Vocabulary CreateCard / CreateProject / AI | Частично режется | — |
| Aggregator | CreateProject → 429; Capture → 413 на ResourceExhausted | CreateCard / BulkCreate → часто 500; коды разные |
| Frontend | `/billing` checkout | Billing скрыт в nav; нет обработки лимита |

Ключевые файлы:

- `VocabularyService/Services/BillingLimitService.cs`
- `VocabularyService/Grpc/ContentService.cs` (projects)
- `VocabularyService/Grpc/CardGrpcService.cs` (Create / Capture / Bulk)
- `AggregatorService/Controllers/ProjectsController.cs`
- `AggregatorService/Controllers/CardsController.cs`
- `polyraspad-frontend/src/app/billing/page.tsx`
- `polyraspad-frontend/src/lib/navigation.ts`

---

## Аудит (2026-07-31) — текущее состояние

**Архитектура:** ✅ Корректная. Лимиты реально применяются.

| Компонент | Статус |
|-----------|--------|
| BillingService: планы Free/Pro, seed | ✅ Готово |
| BillingLimitService (VocabularyService) | ✅ Готово |
| AI-лимиты через Redis | ✅ Готово |
| Fail-open при недоступности Billing | ✅ Готово |
| Webhook idempotency (SHA-256) | ✅ Готово |
| Frontend `/billing` страница | ✅ Готово |
| **Mock checkout не активирует подписку** | 🔴 Блокер |
| `/billing/success` не инвалидирует кэш | 🟡 Проблема |
| `BILLING_WEBHOOK_API_KEY` пустой в .env | 🔴 Prod |
| UI не показывает блокировку при лимите | 🟡 Проблема |
| Capture / BulkCreate не проверяют maxCards | 🔴 Дыра |

---

## Status

| Шаг | Статус |
|-----|--------|
| 0. Исправить Mock-провайдер (блокер тестирования) | **Pending** |
| 1. Vocabulary: закрыть обходы maxCards | Pending |
| 2. Aggregator: единый 402 + error body | Pending |
| 3. Frontend: paywall UX + Billing в nav | Pending |
| 4. Тесты | Pending |
| 5. Manual smoke Free→limit→Pro | Pending |

---

## Шаги реализации

### Шаг 0. Исправить MockPaymentProvider — активация подписки через webhook (БЛОКЕР)

**Проблема:** `MockPaymentProvider.HandleWebhookAsync` логирует и возвращает `WebhookHandleResult.Empty` — никакие события не создаются, подписка остаётся `Incomplete` после checkout.

* **[ ]** В `MockPaymentProvider.HandleWebhookAsync` парсить тело payload (`paymentId`, `status`) и возвращать `PaymentSucceededEvent` при `status == "succeeded"`.
* **[ ]** В `/billing/success/page.tsx` добавить `queryClient.invalidateQueries` после загрузки (useEffect) — план обновится без F5.
* **[ ]** Установить `BILLING_WEBHOOK_API_KEY` в `.env` (любая случайная строка).

**Как проверить после исправления:**
```powershell
$r = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
  -Method POST -ContentType "application/json" -Body '{"email":"you@email.com","password":"pass"}'
$token = $r.token
$c = Invoke-RestMethod -Uri "http://localhost:5000/api/billing/checkout" `
  -Method POST -ContentType "application/json" `
  -Headers @{Authorization="Bearer $token"} -Body '{"planCode":"pro"}'
Invoke-RestMethod -Uri "http://localhost:5000/api/billing/webhooks/mock" `
  -Method POST -ContentType "application/json" `
  -Body (@{paymentId=$c.providerPaymentId; status="succeeded"} | ConvertTo-Json)
# Ожидается planCode="pro":
Invoke-RestMethod -Uri "http://localhost:5000/api/billing/access" -Headers @{Authorization="Bearer $token"}
```

### Шаг 1. Vocabulary — enforcement на всех путях создания карточек

* **[ ]** В `CardGrpcService.CaptureCard` перед созданием:  
  `if (!await _billingLimitService.CanCreateCardAsync(...))` → `RpcException(StatusCode.ResourceExhausted, ...)` с текстом, включающим `maxCards` (как у CreateCard/CreateProject).
* **[ ]** В `BulkCreateCards`:  
  - получить `maxCards` и текущий count;  
  - если `count + request.Cards.Count > max` → ResourceExhausted **до** записи (или создать только доступный остаток — **предпочтительно fail-closed целиком**, проще для UI).
* **[ ]** Убедиться, что `CreateCard` уже вызывает `CanCreateCardAsync` (если нет — добавить).
* **[ ]** Сообщение RPC: единый шаблон, например  
  `Billing limit exceeded: maxCards` / `maxProjects` (парсится Aggregator/FE).

**Out of scope этого шага:** лимит книг/library; AI уже на GenerateContext/ExplainGrammar.

### Шаг 2. Aggregator — единый HTTP-контракт лимита

* **[ ]** Выбрать код: **402 Payment Required** (как в Feature Map). Не смешивать с 429/413 для billing.
* **[ ]** Общий helper (например `BillingLimitHttp.FromRpc`) →  
  `StatusCode = 402`, body:  
  ```json
  { "code": "BILLING_LIMIT_EXCEEDED", "limitKey": "maxCards", "message": "..." }
  ```
* **[ ]** Применить в:
  - `ProjectsController` CreateProject (заменить 429 → 402 для billing ResourceExhausted);
  - `CardsController` CreateCard, CaptureCard, BulkCreateCards;
  - AI endpoints Aggregator, если проксируют Vocabulary AI с ResourceExhausted.
* **[ ]** Не менять семантику других ResourceExhausted (если есть не-billing) — различать по `Detail`/`limitKey` или отдельному prefix в message.

### Шаг 3. Frontend — обнаруживаемость + paywall

* **[ ]** `navigation.ts`: Billing `visible: true` (группа Community / Settings — как удобнее в текущем сайдбаре).
* **[ ]** Shared helper: `isBillingLimitError(response)` → `status === 402` + `code === BILLING_LIMIT_EXCEEDED`.
* **[ ]** UX (минимально): toast или простой dialog Sonner/Radix:  
  «Достигнут лимит тарифа Free» + кнопка **Upgrade** → `/billing`.
* **[ ]** Подключить на путях RF:
  - create project;
  - create card (editor);
  - reader save / capture (mining);
  - import / bulk create.
* **[ ]** Опционально (P2): на `/billing` показывать usage `current/max` из count API или из entitlements + локальных query — не блокер, если нет дешёвого count endpoint.

**Не делать:** редирект всех free-пользователей с dashboard на `/billing`.

### Шаг 4. Тесты

* **[ ]** VocabularyService.Tests: Capture / BulkCreate при `cardCount >= maxCards` → ResourceExhausted; ниже лимита → ok.
* **[ ]** AggregatorService.Tests: mock gRPC ResourceExhausted billing → HTTP 402 + `BILLING_LIMIT_EXCEEDED` для CreateCard / Capture / CreateProject.
* **[ ]** Frontend Vitest: helper распознаёт 402; (по возможности) один тест CTA на mock error в reader/editor.

Команды:

```powershell
dotnet test VocabularyService.Tests/VocabularyService.Tests.csproj -c Release
dotnet test AggregatorService.Tests/AggregatorService.Tests.csproj -c Release
cd polyraspad-frontend; npm test -- --watchAll=false
```

### Шаг 5. Manual smoke

1. Флаги AI/Advanced **false**. Пользователь Free.
2. Создать 3 проекта → 4-й → 402 → Upgrade → `/billing`.
3. Довести карточки до лимита (или временно снизить seed `maxCards` в dev) → Create + Capture + Import → 402 + CTA.
4. Sandbox YooKassa Pro → `/billing/success` → повтор create проходит.
5. Free после cancel/expiry снова упирается в лимит (если легко воспроизвести).

---

## Порядок работ (для агента)

1. Шаг 1 (Vocabulary) + unit tests  
2. Шаг 2 (Aggregator) + integration tests  
3. Шаг 3 (Frontend nav + error UX)  
4. Шаг 5 smoke  
5. Отметить Status Done в этом файле; при необходимости строка в [01_MVP_Completion.md](01_MVP_Completion.md)

---

## Риски

| Риск | Митигация |
|------|-----------|
| Подсчёт `Cards` по `CreatorId` не совпадает с «карточками пользователя в проектах» | Зафиксировать текущую семантику BillingLimitService; не менять формулу в этом плане |
| BulkCreate больших CSV | Fail-closed целиком; в message указать remaining capacity |
| 402 vs 429 ломает существующих клиентов | Только web-app; обновить FE одновременно с Aggregator |
| Fail-open Billing down | Сохранить текущий fallback free limits (NFR); не делать fail-closed без отдельного решения |

---

## Definition of Done

- [ ] Free не может обойти `maxCards` через reader capture или import  
- [ ] Все billing-отказы с Aggregator = **402** + `BILLING_LIMIT_EXCEEDED`  
- [ ] UI ведёт на `/billing`; пункт Billing виден в nav  
- [ ] Автотесты Vocabulary + Aggregator зелёные  
- [ ] Manual smoke Free → limit → Pro пройден  
