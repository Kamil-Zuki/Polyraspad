# Фича: Интерактивный Ридер и Майнинг Фраз (Reader & Sentence Mining)

**Статус:** Implemented  
**Связанный бекенд:** Vocabulary Service (`TextService.AnalyzeText`, `TermService.MarkTermKnown`, `CardService.CreateCard`, `AIService.ExplainGrammar`), Media Service.

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Выбор документа.** Пользователь открывает книгу или загруженный документ в `/reader?bookId=...`.
* **Шаг 2: Токенизация и рендеринг.** Текст страницы отправляется в `AnalyzeText`. Возвращается массив токенов с цветами статусов (`NEW` blue, `SAVED` yellow, `KNOWN` white, `IGNORED` muted).
* **Шаг 3: Инспекция термина.** Клиент нажимает на токенизированное слово или выделяет фразу. В правой боковой панели `<TermInspector>` отображаются переводы, озвучка TTS и грамматический разбор (`ExplainGrammar`).
* **Шаг 4: Майнинг карточки (Sentence Mining).** Пользователь нажимает "Save Card". Контекстное предложение и перевод отправляются на создание карточки (`CreateCard`) с привязкой к текущей колоде.
* **Шаг 5: Перелистывание страницы.** При переходе на следующую страницу кликом по стрелке или `PageDown` оставшиеся синие токены (`NEW`) опционально отправляются в `BulkMarkKnown`.

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/reader/page.tsx` (Client Component) — главный экран интерактивного чтения.
* `src/app/library/page.tsx` (Server Component) — библиотека загруженных книг и документов.

---

## 3. Дерево компонентов (Component Architecture)

```
<ReaderPage> (Client)
├── <ReaderHeader> — прогресс чтения, номер страницы, настройки авто-пометки
├── <TextCanvas> — холст с токенизированными словами и сохраненными фразами
│   └── <TokenWord> — отдельное интерактивное слово с индикацией цвета статуса
└── <TermInspector> — боковая панель с деталями термина
    ├── <TermStatusSelector> — кнопки переключения (NEW, SAVED, KNOWN, IGNORED)
    ├── <TtsAudioPlayer> — кнопка озвучки
    ├── <AiGrammarCard> — AI-объяснение грамматики
    └── <QuickCardCreator> — форма быстрого создания карточки
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **Чтение (Queries):**
  * `POST /api/v1/terms/analyze` (`TextService.AnalyzeText`) — получение токенов страницы.
  * `GET /api/v1/terms/{id}` (`TermService.GetTermDetails`) — подробная информация по термину.
* **Мутации (Mutations):**
  * `POST /api/v1/terms/mark-known` (`TermService.MarkTermKnown`) — пометка выученным.
  * `POST /api/v1/terms/bulk-mark-known` (`TermService.BulkMarkKnown`) — пакетная пометка при смене страницы.
  * `POST /api/v1/cards` (`CardService.CreateCard`) — майнинг карточки из ридера.
  * `POST /api/v1/ai/explain-grammar` (`AIService.ExplainGrammar`) — AI разбор грамматики.

---

## 5. Управление состоянием (State Management)

* **Локальное состояние:**
  * `selectedToken`: выбранный токен текста в `<TextCanvas>`.
  * `pageIndex`: текущая страница документа.
* **Кэш React Query (`@/lib/react-query`):**
  * Ключи `['reader', 'tokens', projectId, pageIndex]` — кэширование результатов токенизации. Инвалидация при мутации статуса термина.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/reader/reader.test.tsx`):**
  * Проверка рендеринга синих/желтых/белых слов по полученному ответу `AnalyzeText`.
  * Проверка открытия `<TermInspector>` при клике на токен.
  * Проверка вызова `BulkMarkKnown` при листании страницы.
