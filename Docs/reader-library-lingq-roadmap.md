# Reader/Library → LingQ Roadmap

**Цель:** Закрыть разрывы между текущим состоянием и LingQ-style UX

**Ориентир:** [LingQ Website](https://www.lingq.com/)

## Текущее состояние (Current)

### Есть (База)
- ✅ Reader UI с анализом текста/PDF
- ✅ Токенизация и подсветка статусов
- ✅ Term inspector (базовый)
- ✅ gRPC endpoints в VocabularyService
- ✅ VocabularyServiceClient в Aggregator
- ✅ Активный план LingQ-направления

### Критические разрывы (Gaps)
- ❌ **REST Bridge:** Нет TextController, TermsController, MediaController
- ❌ **Bulk Known:** Endpoint есть, но не подключен в UI
- ❌ **Phrase Workflow:** В UI захардкожен type="WORD"
- ❌ **Review из Reader:** Нет интеграции с SRS
- ❌ **Continue Reading:** Нет resume для PDF
- ❌ **Content Library:** Library остается deck-first

---

## Roadmap

### Phase 0: Foundation (2-3 недели)
**Критично для работы end-to-end**

| Задача | Сложность | Риск | Овнер |
|--------|-----------|------|-------|
| Создать `MediaServiceClientImpl` | Medium | High | Backend |
| Создать `TextController` + `/api/text/analyze` | Medium | Medium | Backend |
| Создать `TermsController` + все term endpoints | Medium | Medium | Backend |
| Создать `MediaController` + reader library endpoints | High | High | Backend |
| Исправить `MediaServiceClientImpl` регистрацию в DI | Low | Low | Backend |

**Критерии готовности Phase 0:**
- [ ] Все endpoints из `constants.ts` возвращают 200 (не 404)
- [ ] Frontend может анализировать текст через Aggregator
- [ ] Frontend может создавать термины
- [ ] Frontend может получать библиотеку книг

**Definition of Done:**
```bash
# Integration tests проходят
curl -X POST /api/text/analyze → 200
curl -X POST /api/terms → 201
curl -X GET /api/Media/library/xxx → 200
```

---

### Phase 1: Core LingQ Experience (3-4 недели)
**Основной UX flow**

| Задача | Сложность | Риск | Овнер |
|--------|-----------|------|-------|
| Подключить `bulkMarkKnown` в reader page turn | Medium | Low | Frontend |
| Добавить user setting "Mark blue as known" | Low | Low | Frontend + Backend |
| Реализовать phrase selection (Shift+click) | High | Medium | Frontend |
| Поддержать `type: "PHRASE"` во всех term мутациях | Medium | Medium | Frontend + Backend |
| Приоритет отображения фраз над словами | Medium | High | Frontend |
| Resume reading для PDF (lastPageNumber) | Medium | Medium | Frontend |

**Критерии приемки Phase 1:**
- [ ] Пользователь может создать phrase LingQ
- [ ] Перелистывание помечает синие как known (по настройке)
- [ ] Открытие PDF возобновляет с последней страницы
- [ ] Фразы отображаются корректно поверх слов

**Definition of Done:**
- [ ] Все acceptance criteria из `lingq-style-acceptance-criteria.md` для core experience выполнены
- [ ] Регрессионные тесты проходят

---

### Phase 2: Review Integration (2-3 недели)
**SRS связка с Reader**

| Задача | Сложность | Риск | Овнер |
|--------|-----------|------|-------|
| API для "Review из текущего контекста" | Medium | Medium | Backend |
| Счетчик "Review: N" в reader header | Low | Low | Frontend |
| Модальное окно/переход в SRS из reader | Medium | Low | Frontend |
| Возврат в reader после review | Medium | Low | Frontend |
| Контекст-awarness для карточек (source URL) | Medium | Medium | Backend |

**Критерии приемки Phase 2:**
- [ ] В reader виден счетчик due карточек
- [ ] Нажатие запускает review-сессию
- [ ] После review пользователь возвращается в reader
- [ ] Карточки помнят контекст (предложение, источник)

---

### Phase 3: Content-First Library (3-4 недели)
**Реорганизация Library**

| Задача | Сложность | Риск | Овнер |
|--------|-----------|------|-------|
| Новая IA: Continue Reading, My Books, Collections | Medium | High | Frontend |
| Book cards с real progress (не placeholder) | Medium | Medium | Frontend |
| Continue Reading algorithm | Medium | Medium | Frontend + Backend |
| Import flow improvements | Medium | Low | Frontend |
| Collections management UI | Medium | Low | Frontend |

**Критерии приемки Phase 3:**
- [ ] Dashboard показывает Continue Reading
- [ ] Library единая точка входа
- [ ] Book cards показывают реальный прогресс
- [ ] Импорт ведет сразу в reader

---

### Phase 4: Polish & Performance (2 недели)
**Оптимизация и стабилизация**

| Задача | Сложность | Риск | Овнер |
|--------|-----------|------|-------|
| Кэширование анализа текста | Medium | Medium | Backend |
| Virtual scrolling для длинных текстов | Medium | Medium | Frontend |
| Optimistic UI для term actions | Low | Low | Frontend |
| Performance monitoring | Medium | Low | Backend |
| Accessibility audit | Medium | Medium | Frontend |

---

### Phase 5: Advanced Features (Future)
**Дополнительный функционал**

| Задача | Сложность | Примечание |
|--------|-----------|------------|
| Sentence View (sentence-by-sentence) | High | Этап 10 из плана |
| Multi-context для терминов | Medium | TermOccurrence |
| YouTube/Video import | High | Требует транскрипцию |
| Mobile-optimized reader | Medium | Touch gestures |
| Offline mode | High | Service workers |

---

## Приоритизация по Impact/Effort

```
                    HIGH IMPACT
                         │
    ┌────────────────────┼────────────────────┐
    │   resume PDF      │  review from       │
    │   (Phase 1)       │  reader (Phase 2)  │
    │                   │                    │
LOW ├────────────────────┼────────────────────┤ HIGH
EFF │   bulk known      │  phrase LingQ     │ EFF
ORT │   (Phase 1)       │  (Phase 1)         │ ORT
    │                   │                    │
    │   REST bridge     │  Content-first     │
    │   (Phase 0)       │  Library (Phase 3) │
    └────────────────────┼────────────────────┘
                         │
                    LOW IMPACT
```

**Quick Wins (низкий effort, высокий impact):**
- REST Bridge Phase 0 (блокер всего)
- Bulk known настройка
- Resume PDF

**High Investment (высокий effort, высокий impact):**
- Phrase workflow полностью
- Content-first Library реорганизация
- Review integration

---

## Метрики прогресса

| Phase | Lead Metric | Lag Metric | Target |
|-------|-------------|------------|--------|
| 0 | Endpoints implemented | Integration tests passing | 100% |
| 1 | User actions completed | Time to first LingQ | < 30s |
| 2 | Review sessions started | Review completion rate | > 70% |
| 3 | Books opened | Continue Reading usage | > 50% |
| 4 | Performance scores | Lighthouse score | > 90 |

---

## Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| MediaServiceClientImpl не компилируется | Высокое | Критичное | Создать stub, инкрементально добавлять методы |
| Phrase UI сложнее ожидаемого | Среднее | Высокое | Показать предупреждение при превышении length |
| Backend API breaking changes | Низкое | Среднее | Версионирование API v1/v2 |
| Performance анализа текста | Среднее | Высокое | Кэширование, pagination, streaming |
| Регрессия существующего функционала | Среднее | Среднее | Полный regression test suite перед merge |

---

## Зависимости

```
Phase 0 (REST Bridge)
       │
       ├──→ Phase 1 (Core Experience)
       │         │
       │         ├──→ Phase 2 (Review)
       │         │
       │         └──→ Phase 3 (Library IA)
       │
       └──→ Phase 4 (Performance)
```

**Критический путь:** Phase 0 → Phase 1 → Phase 2

---

## Ресурсы

### Документация
- `Docs/architecture/aggregator-bridge-audit.md`
- `Docs/reader/reader-product-spec-v2.md`
- `Docs/library/library-content-first-ia.md`
- `Docs/api/reader-aggregator-contract.md`
- `Docs/ux/lingq-style-acceptance-criteria.md`
- `Docs/testing/reader-library-tdd-matrix.md`

### Код
- Frontend: `polyraspad-frontend/src/app/reader/`
- Backend: `AggregatorService/Controllers/` (создать)
- gRPC: `VocabularyService/Grpc/`

### Внешние ориентиры
- [LingQ Method](https://www.lingq.com/)
- Active plan: `context/plans/active/lingq-reader-implementation-plan.md`

---

## Definition of Overall Done

Проект считается завершенным когда:

1. ✅ Все endpoints работают через Aggregator
2. ✅ Пользователь может создать LingQ, Known, Ignore
3. ✅ Phrase workflow работает end-to-end
4. ✅ Review доступен из reader
5. ✅ Library показывает continue reading
6. ✅ Resume PDF работает
7. ✅ Все acceptance criteria выполнены
8. ✅ Performance targets met
9. ✅ Test coverage > 75%
10. ✅ Documentation полная и актуальная
