# Основные сущности: Активность и Оценка Навыков (Activity & Assessment)

Данная группа сущностей отвечает за отслеживание активности пользователя по 4 ключевым навыкам (Reading, Listening, Writing, Speaking), расчет уровней навыков и фиксацию результатов оценок от ИИ-репетитора. Сюда же относится сохранение индивидуальных попыток речевой практики (Shadowing).

*Примечание: детальное описание сущностей `SkillType`, `UserSkillActivity` и `UserSkillProgress` можно найти в файле `Skill_Tracking.md`.*

---

## 1. ShadowingAttempt

`ShadowingAttempt` — сущность, фиксирующая отдельную попытку пользователя повторить предложение вслух. Используется как основа для пополнения ежедневной активности по навыку говорения (`speaking`) и для отслеживания прогресса произношения.

**Поля:**
- `Id` (Guid, PK)
- `UserId` (Guid) — идентификатор пользователя.
- `CardId` (Guid?, FK to Card) — связь с изучаемой карточкой (опционально).
- `SourceBookId` (string?) — идентификатор книги или документа, если Shadowing был запущен напрямую из читалки (опционально).
- `SentenceText` (string) — текст исходного предложения.
- `TtsAudioUrl` (string) — ссылка на эталонное аудио (сгенерированное TTS).
- `UserRecordingUrl` (string?) — ссылка на загруженную запись голоса пользователя из MediaService.
- `SelfRating` (int) — оценка пользователя: `1=Bad`, `2=Okay`, `3=Good`.
- `CreatedAt` (DateTime) — когда была совершена попытка.

**Использование:**
Каждая созданная попытка должна увеличивать показатель `Value` для сегодняшней записи `UserSkillActivity` с типом `speaking`.

---

## 2. SkillAssessmentLog

`SkillAssessmentLog` — журнал срезов уровня владения навыками, которые фиксируются в ходе тестирования или ролевых игр с ИИ-агентом.

**Поля:**
- `Id` (Guid, PK)
- `UserId` (Guid) — идентификатор пользователя.
- `ProjectId` (Guid) — идентификатор текущего проекта изучения языка.
- `Skill` (string) — строковый код навыка (`reading`, `listening`, `writing`, `speaking`).
- `Score` (int) — полученный балл (оценка).
- `CreatedAt` (DateTime) — когда был проведен срез.

*(Эта сущность в дальнейшем будет расширяться для поддержки детального фидбэка, связи с `AgentThreadId` и оценки CEFR, см. Gap Analysis).*

---

## Связь сущностей в рамках группы 4

```mermaid
erDiagram
    Card ||--o{ ShadowingAttempt : triggers
    User ||--o{ ShadowingAttempt : records
    User ||--o{ SkillAssessmentLog : evaluated_through
    Project ||--o{ SkillAssessmentLog : tracks_for
```
