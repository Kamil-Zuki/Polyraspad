# SR-VOC-06: Активность и Оценка Навыков (Activity & Assessment)

## Описание
Данный раздел описывает требования к учету ежедневной активности по четырем ключевым навыкам (Reading, Listening, Writing, Speaking), расчету прогресса ежедневных миссий, анализу устных попыток произношения (Shadowing) и периодическому тестированию грамматики и речи с помощью AI-агентов.

---

## Возможности данного раздела

| Код | Название и Описание |
| :--- | :--- |
| **SR-VOC-ACT-01** | **Ежедневные миссии (Daily Missions):** Фиксация минут чтения/аудирования и количества упражнений письма/говорения для выполнения дневных норм. |
| **SR-VOC-ACT-02** | **Накопительный прогресс навыков (Skill Progression):** Расчет общего уровня пользователя по каждому навыку (от 0 до 100) на основе суммарной активности. |
| **SR-VOC-ACT-03** | **Оценка речевой практики Shadowing (Shadowing Practice):** Запись голоса, сопоставление с эталоном TTS, оценка произношения и инкремент навыка Speaking. |
| **SR-VOC-ACT-04** | **AI-срезы уровня (AI Skills Assessment):** Фиксация результатов периодических срезов знаний от ИИ-тьютора в журнале оценок. |
| **SR-VOC-ACT-05** | **ExplainGrammar (без persistence тем):** RPC объяснения грамматики; сущностей `GrammarTopic` / `UserGrammarProgress` нет. |
| **SR-VOC-ACT-06** | **TrackSkillActivity:** инкремент ежедневной активности по навыку. |
| **SR-VOC-ACT-07** | **Analytics:** VocabularyStats, Heatmap, DailySummary, SkillBalance, AssessmentHistory. |
| **SR-VOC-ACT-08** | **Daily Autopilot Plan:** `GetDailyAutopilotPlan` — план дня на основе прогресса/активности. |
| **SR-VOC-ACT-09** | **UserBookProgress:** позиция чтения книги (см. также `SR-VOC-READ-01`). |

---

# Детальная спецификация требований

## SR-VOC-ACT-01: Ежедневные миссии {#SR-VOC-ACT-01}
### 1. Цель и ключевые принципы
- **Разделение типов активности:** Чтение (`reading`) и аудирование (`listening`) измеряются в минутах. Письмо (`writing`) и говорение (`speaking`) измеряются в выполненных упражнениях (exercises).
- **Сброс суток:** Активность накапливается в `UserSkillActivity` и обнуляется в конце суток согласно `RolloverHour` пользователя.

---

## SR-VOC-ACT-02: Накопительный прогресс навыков {#SR-VOC-ACT-02}
### 1. Цель и ключевые принципы
- **Неубывающий прогресс:** В отличие от ежедневных миссий, общий прогресс в `UserSkillProgress` постоянно суммируется.
- **Расчет уровня:** Специальная формула переводит общее время/упражнения в уровень навыка (Level 1..100).

---

## SR-VOC-ACT-03: Оценка речевой практики Shadowing {#SR-VOC-ACT-03}
### 1. Цель и ключевые принципы
- **Модель данных:** EF-сущность `ShadowingAttempt` хранит эталон TTS, запись пользователя и self-rating.
- **Ограничение кода:** отдельного gRPC CRUD для Shadowing в текущем VocabularyService **не найдено** — персистентность подготовлена, публичный контракт может отсутствовать (см. staging ISSUE).

---

## SR-VOC-ACT-04: AI-срезы уровня {#SR-VOC-ACT-04}
### 1. Цель и ключевые принципы
- **AI-аудит:** результаты срезов пишутся в `SkillAssessmentLog` и доступны через analytics/history API.

---

## SR-VOC-ACT-05: ExplainGrammar {#SR-VOC-ACT-05}
### 1. Цель и ключевые принципы
- **Без справочника тем:** RPC `ExplainGrammar` возвращает объяснение; таблиц `GrammarTopic` / `UserGrammarProgress` нет.
- Не следует описывать ConfidenceScore persistence как реализованный домен.

---

## SR-VOC-ACT-06: TrackSkillActivity {#SR-VOC-ACT-06}
### 1. Цель и ключевые принципы
- Инкремент `UserSkillActivity.Value` по `SkillType` за день (UTC / rollover).

---

## SR-VOC-ACT-07: Analytics {#SR-VOC-ACT-07}
### 1. Цель и ключевые принципы
- VocabularyStats, Heatmap, DailySummary, SkillBalance, AssessmentHistory — read-модели для дашборда.

---

## SR-VOC-ACT-08: Daily Autopilot Plan {#SR-VOC-ACT-08}
### 1. Цель и ключевые принципы
- `GetDailyAutopilotPlan` формирует план дня на основе прогресса/активности проекта.

---

## SR-VOC-ACT-09: UserBookProgress {#SR-VOC-ACT-09}
### 1. Цель и ключевые принципы
- Сохранение позиции чтения по внешнему `BookId` (детально — `SR-VOC-READ-01`).
