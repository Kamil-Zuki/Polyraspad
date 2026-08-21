# DTO: Основные и Доменные структуры (Vocabulary Core)

Данный документ описывает базовые DTO и Protobuf-сообщения микросервиса `Vocabulary Service`.

---

## 1. Project & User Settings DTOs

### `ProjectResponse`
- `id` (Guid / string): Идентификатор проекта.
- `title` (string): Название проекта (например, "Английский — Книги").
- `source_lang` (string): Исходный язык ISO (например, "ru").
- `target_lang` (string): Изучаемый язык ISO (например, "en").
- `settings` (`SrsSettings`): Параметры FSRS-алгоритма повторений.
- `stats` (`ProjectStats`): Кэш статистики изученных терминов (`total_lemmas`, `mature_lemmas`).
- `is_archived` (bool): Флаг архивного состояния.

### `UserSettingsResponse`
- `user_id` (Guid / string): Идентификатор пользователя.
- `rollover_hour` (int): Час смены учебного дня (0-23, дефолт 4 AM).
- `daily_goal_new` (int): Цель по новым карточкам в день (дефолт 20).
- `daily_goal_review` (int): Цель по повторениям в день (дефолт 100).
- `interface_language` (string): Язык UI.
- `current_streak` (int), `max_streak` (int): Серия дней активности.

---

## 2. Deck & Card DTOs

### `DeckTreeItem`
- `id` (string): UUID колоды.
- `title` (string): Название колоды.
- `card_count` (int): Общее количество карточек.
- `children` (repeated `DeckTreeItem`): Вложенные дочерние колоды.
- `stats` (`DeckDetailStats`): Своевременные счетчики (`new_cards_count`, `learning_cards_count`, `due_cards_count`, `studyable_now_count`).

### `CardResponse`
- `id` (string): UUID карточки.
- `deck_id` (string): UUID родительской колоды.
- `srs_status` (enum `SrsStatus`): `SRS_STATUS_NEW` (0), `SRS_STATUS_LEARNING` (1), `SRS_STATUS_REVIEW` (2), `SRS_STATUS_RELEARNING` (4).
- `note` (`NotePayload`): Полезная нагрузка заметки и значений полей.
- `active_card_template` (`CardTemplatePayload`): Активный шаблон карточки.
