# Фича: Аналитика, Автопилот и Shadowing (Analytics, Autopilot & Shadowing)

**Статус:** Implemented  
**Связанный бекенд:** Vocabulary Service (`AnalyticsService`, `AutonomyService.GetDailyAutopilot`, `GetNextBestActions`), Agent Service.

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Командный центр (`/dashboard`).** Пользователь просматривает виджет автопилота с предложенными дневными задачами (Next Best Actions): "20 New Cards", "15 min Reading", "1 Lesson".
* **Шаг 2: Аналитика и Радар Навыков (`/analytics`).** Пользователь открывает панель аналитики. Видит радар навыков (Reading, Listening, Writing, Speaking), графики ретеншна FSRS и распределение терминов по статусам.
* **Шаг 3: Практика Shadowing (`/shadowing`).** Пользователь выбирает сохраненную фразу, прослушивает эталонное аудио (TTS), записывает свою речь с микрофона и получает оценку сходства произношения.

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/dashboard/page.tsx` — командная панель и автопилот дня.
* `src/app/analytics/page.tsx` — радар навыков и статистика.
* `src/app/shadowing/page.tsx` — интерактивный тренажер произношения Shadowing.

---

## 3. Дерево компонентов (Component Architecture)

```
<DashboardPage> (Client)
├── <StreakWidget> — счетчик дней активности подряд (Streak)
├── <DailyAutopilotCard> — рекомендованный план автопилота дня
└── <NextBestActionsList> — список приоритетных задач (NextBestAction)

<AnalyticsPage> (Client)
├── <SkillRadarChart> — диаграмма навыков (Recharts / SVG Radar)
├── <FsrsRetentionGraph> — график удержания памяти
└── <TermDistributionDonut> — круговая диаграмма статусов (NEW/SAVED/KNOWN/IGNORED)

<ShadowingPage> (Client)
├── <AudioReferencePlayer> — плеер оригинального звучания
├── <MicRecorder> — кнопка записи голоса
└── <PronunciationScoreCard> — визуализация оценки сжатия и тональности
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **Чтение (Queries):**
  * `GET /api/v1/analytics/dashboard` (`AnalyticsService.GetDashboardStats`) — статистика дашборда.
  * `GET /api/v1/autonomy/autopilot` (`AutonomyService.GetDailyAutopilot`) — план автопилота дня.
  * `GET /api/v1/analytics/radar` (`AnalyticsService.GetSkillRadar`) — данные радара навыков.
* **Мутации (Mutations):**
  * `POST /api/v1/shadowing/submit` — отправка записи попытки Shadowing.

---

## 5. Управление состоянием (State Management)

* **Локальное состояние:**
  * `isRecording`: статус записи с микрофона на странице `/shadowing`.
* **Кэш React Query:**
  * `['analytics', 'dashboard', projectId]` — кэш показателей активности.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/analytics/analytics.test.tsx`):**
  * Проверка рендеринга виджета Стрика и корректного расчёта процентов ежедневных целей.
  * Проверка отображения списка Next Best Actions в блоке автопилота.
