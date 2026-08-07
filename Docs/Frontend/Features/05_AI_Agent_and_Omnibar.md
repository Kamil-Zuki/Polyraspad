# Фича: AI-Ассистент, Omnibar и Инструменты (AI Agent, Omnibar & Tools)

**Статус:** Implemented  
**Связанный бекенд:** Agent Service, Vocabulary Service (`AIService.GenerateContext`, `ExplainGrammar`), Next.js BFF (`/api/ai/*`).

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Вызов Omnibar.** Из любого экрана приложения пользователь нажимает `Ctrl+K` (или `Cmd+K`). Открывается модальное окно `<Omnibar />`.
* **Шаг 2: Выполнение быстрых команд.**
  * Ввод текста с префиксом `Add "apple"` — создание карточки слова.
  * Ввод вопроса "Explain grammar: slept" — быстрый AI-разбор.
  * Навигация по страницам приложения с помощью стрелок клавиатуры и `Enter`.
* **Шаг 3: Чат с AI-агентом (`/agents`).** Пользователь переходит в полноэкранную рабочую область Agent Workspace. Выбирает или создает поток диалога (`thread`).
* **Шаг 4: Генерация карточек по теме (`/generator`).** Пользователь запрашивает у AI сгенерировать набор карточек по заданной теме или тексту (например, "Разговор во французском кафе, уровень A2").

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/agents/page.tsx` — полноэкранная рабочая область AI-агентов (Agent Workspace).
* `src/app/generator/page.tsx` — генератор карточек на базе LLM.
* `src/app/api/ai/generate/route.ts` — BFF API прокси маршрут генерации ответов.

---

## 3. Дерево компонентов (Component Architecture)

```
<OmnibarProvider> (Global Context)
└── <OmnibarModal> — модальная палитра команд (Radix Dialog)
    ├── <CommandInput> — поле ввода с автодополнением
    └── <CommandList> — группы результатов (Navigation, Quick Actions, AI Prompts)

<AgentWorkspacePage> (Client)
├── <ThreadSidebar> — список потоков диалогов пользователя
└── <AgentChatContainer> — чат текущего треда
    ├── <MessageList> — сообщения пользователя и агента
    └── <AgentInputBox> — ввод с поддержкой прикрепления контекста
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **BFFМаршруты (`/api/ai/*`):**
  * `POST /api/ai/generate` — проксирование вызова генерации ответа LLM.
  * `GET /api/ai/models` — список доступных моделей.
* **gRPC / REST Инструменты:**
  * `POST /api/v1/ai/generate-context` (`AIService.GenerateContext`) — примеры предложений.
  * `POST /api/v1/ai/explain-grammar` (`AIService.ExplainGrammar`) — объяснение грамматики.

---

## 5. Управление состоянием (State Management)

* **Глобальный контекст (`OmnibarContext`):**
  * `isOpen` (boolean): состояние видимости Omnibar.
  * `query` (string): текущий поисковый запрос.
* **Локальное состояние:**
  * `activeThreadId`: идентификатор текущего диалога в Agent Workspace.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/omnibar/omnibar.test.tsx`):**
  * Проверка открытия Omnibar по сочетанию клавиш `Ctrl+K` и `Cmd+K`.
  * Проверка фильтрации команд при вводе поискового запроса.
  * Проверка навигации по маршрутам при выборе пункта с клавиатуры.
