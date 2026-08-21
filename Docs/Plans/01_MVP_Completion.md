# План реализации Этапа 1 (MVP)

**Цель:** Выпуск первой стабильной версии Polyraspad в РФ. Запуск основного цикла обучения (LingQ-reader + FSRS повторение) с приемом платежей через ЮKassa и скрытием функционала будущих этапов.

**Продуктовый контекст:** [01Feature_Map.md](../Product/01Feature_Map.md) (Stage 1).  
**OCR:** код Phase 1 — [02_OCR_Phase1_Status.md](02_OCR_Phase1_Status.md) (manual QA отдельно).  
**Freemium limits / paywall (после checkout):** [03_Freemium_Limits_Paywall.md](03_Freemium_Limits_Paywall.md).

**Scope RF MVP (2026-07-22):** web-app only — Library/Reader + FSRS + click-to-translate + YooKassa + feature gating.  
**Chrome Capture Extension (Шаг 4) — Deferred:** не блокирует RF launch; backend `POST /api/Cards/capture` остаётся для in-app mining.

> **Shadowing:** UI `/shadowing` удалён из frontend (2026-07-22); будущая реализация будет отдельным дизайном. Исторический UX-набросок: [reader-library-shadowing-mvp.md](../Product/reader-library-shadowing-mvp.md) (не авторитетен для RF Stage 1).

---

## Status

| Область | Статус | Примечание |
|---------|--------|------------|
| Feature flags (env + nav) | Done | `NEXT_PUBLIC_FF_*`, nav/omnibar hide |
| Feature flags (reader mining / TTS API) | Done | mining gated; soft-redirects; GenerateAudio gated |
| Features config + filter Agent/AI/Lessons/Community | Done | `Features` в appsettings; `FeatureFlagFilter` |
| Public/Downloaded deck UI | Done | Gated via Advanced Modules; fork/download not in Stage 1; Decks Create/Update clamps `IsPublic` |
| YooKassa provider + webhook + checkout | Done | Free / **Pro** (не Premium) |
| Billing success refresh | Done | `/billing/success` invalidates + shows plan/pending |
| Reader save → SAVED + exact forms | Done | LingQ regression tests |
| FSRS study trainer | Done | `/study` + inclusive |
| FSRS → reader KNOWN (white) | Done | Good/Easy + Review → `KNOWN` + termStatusEpoch |
| OCR Phase 1 (code) | Done | manual QA checklist open |
| Capture Extension client | **Deferred** | Anki-only; out of RF MVP |

---

## Remaining tasks

### Шаг 1. Feature Flags

* **[x]** Frontend: mining drafts / inspector AI gated; soft-redirect `/agents`, `/lessons`, `/marketplace`; Shadowing route removed.
* **[x]** Backend: `GenerateAudio` gated; `FeaturesOptions` defaults `false`.
* **[x]** Public/Downloaded: filters, Make public, contribution policy, Public/Purchased badges hidden when Advanced off; Aggregator clamps `IsPublic` / ignores `ContributionPolicy` on Decks Create/Update.
* Browser TTS + click-to-translate при выключенном AI — **оставить**.

### Шаг 2. ЮKassa

* **[x]** Backend: provider `yookassa`, Aggregator webhook proxy.
* **[x]** Frontend: `/billing` Free/Pro, checkout redirect.
* **[x]** `/billing/success`: refetch access/subscription (или явный pending webhook).

### Шаг 3. Reader & Vocabulary

* **[x]** Save → `SAVED`; sleep/slept раздельно.
* **[x]** `SubmitReview` Good/Easy + FSRS state Review → `UserTermStatus.Status = KNOWN`; invalidate reader queries.

### Шаг 4. Chrome-расширение — Deferred

* Backend capture + duplicate update — готовы для web mining.
* Extension → JWT + Aggregator — **не в RF MVP**.

---

## План верификации

### Автоматические тесты

```powershell
dotnet test AggregatorService.Tests/AggregatorService.Tests.csproj -c Release
dotnet test VocabularyService.Tests/VocabularyService.Tests.csproj -c Release
dotnet test BillingService.Tests/BillingService.Tests.csproj -c Release
cd polyraspad-frontend; npm test -- --watchAll=false
```

### Ручной smoke (без extension)

1. Флаги `false`: нет Agents/Lessons/Marketplace в nav; `/shadowing` отсутствует; reader mining AI не стреляет; `generate-audio` → 404; browser TTS + translate работают.
2. Save слово → жёлтый; Good до Review в study → в reader белый (KNOWN) после повторного analyze / возврата в reader.
3. Sandbox YooKassa → `/billing/success` → access обновляется или явный pending.
4. OCR checklist из [02_OCR_Phase1_Status.md](02_OCR_Phase1_Status.md).

### Автопроверка (2026-07-22)

- AggregatorService.Tests: 57 passed (incl. FeatureFlagFilter + GenerateAudio 404)
- VocabularyService.Tests Study/Term/AnkiFsrs + KnownStatusSync: passed
- polyraspad-frontend `reader/page.test.tsx`: 18 passed
