# DTO: Заметки, Динамические Поля и Параметры FSRS

Данный документ описывает DTO заметок, динамических полей Anki-стиля и параметров алгоритма FSRS.

---

## 1. Note & Template Payloads

### `NotePayload`
- `id` (string): UUID заметки.
- `note_type_id` (string): UUID типа заметки (`NoteType`).
- `field_values` (`map<string, NoteFieldValuePayload>`): Словарь значений полей по ключу `FieldKey`.
- `project_term_id` (string?): Ссылка на привязанный термин точной формы (`ProjectTerm`).

### `NoteFieldDefinitionPayload`
- `field_key` (string): Уникальный ключ поля (например, "Expression", "Translation").
- `label` (string): Название поля для отображения в форме ввода.
- `field_type` (string): Тип ввода (`text`, `textarea`, `tags`, `image`, `audio`, `url`).
- `sort_order` (int): Порядок сортировки.
- `required` (bool), `archived` (bool).

### `CardTemplatePayload`
- `template_key` (string): Ключ шаблона (например, "Forward").
- `front_template` (string): HTML/Mustache шаблон лицевой стороны (`{{Expression}}`).
- `back_template` (string): HTML/Mustache шаблон обратной стороны (`{{Translation}}`).

---

## 2. FSRS Settings & Progress DTOs

### `SrsSettings`
- `request_retention` (double): Желаемый процент удержания знаний (дефолт 0.90 / 90%).
- `maximum_interval` (int): Максимальный интервал повторения в днях (дефолт 36500).
- `w` (repeated double): 19 коэффициентов весов FSRS.
- `learning_steps_seconds` (repeated int): Шаги первичного изучения в секундах (например, `[60, 600]`).
- `relearning_steps_seconds` (repeated int): Шаги переизучения после ошибочного ответа (например, `[600]`).
- `enable_fuzzing` (bool): Флаг включения случайного разброса интервалов (fuzzing).
