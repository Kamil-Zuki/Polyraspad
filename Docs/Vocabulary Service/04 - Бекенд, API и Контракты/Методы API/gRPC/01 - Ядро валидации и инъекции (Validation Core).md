# gRPC Методы: ContentService, CardService и TermService

Данный документ содержит спецификацию методов управления доменным контентом (проекты, колоды, настройки), карточками заметок и терминами словаря.

---

## 1. ContentService (Проекты, Настройки, Колоды)

### CreateProject
- **Сигнатура:** `rpc CreateProject (CreateProjectRequest) returns (ProjectResponse)`
- **Требование:** SR-STR-01 / SR-VOC-04
- **Описание:** Создает новый языковой проект пользователя (`title`, `source_lang`, `target_lang`, базовые FSRS-настройки).

### GetProjects
- **Сигнатура:** `rpc GetProjects (GetProjectsRequest) returns (GetProjectsResponse)`
- **Требование:** SR-STR-01
- **Описание:** Возвращает список всех проектов текущего пользователя (с фильтрацией архивированных).

### GetProjectDetails / UpdateProject
- **Сигнатура:** `rpc GetProjectDetails (GetProjectDetailsRequest) returns (ProjectResponse)`
- **Сигнатура:** `rpc UpdateProject (UpdateProjectRequest) returns (ProjectResponse)`
- **Требование:** SR-STR-02
- **Описание:** Получение подробностей проекта и обновление его параметров (название, архивный статус, FSRS-веса).

### GetUserSettings / UpdateUserSettings
- **Сигнатура:** `rpc GetUserSettings (GetUserSettingsRequest) returns (UserSettingsResponse)`
- **Сигнатура:** `rpc UpdateUserSettings (UpdateUserSettingsRequest) returns (UserSettingsResponse)`
- **Требование:** SR-SETT-01
- **Описание:** Получение и обновление глобальных настроек пользователя (час сброса rollover, дневные цели New/Review, язык интерфейса).

### GetDeckTree / GetDeckDetail
- **Сигнатура:** `rpc GetDeckTree (GetDeckTreeRequest) returns (GetDeckTreeResponse)`
- **Сигнатура:** `rpc GetDeckDetail (GetDeckDetailRequest) returns (GetDeckDetailResponse)`
- **Требование:** SR-STR-03
- **Описание:** Формирование иерархического дерева колод с фильтрами (`MINE`, `DOWNLOADED`, `PUBLIC`) и детальной статистикой карточек (New, Learning, Due).

---

## 2. CardService (Карточки, Заметки и Загрузка Медиа)

### CreateCard / UpdateCard / DeleteCard
- **Сигнатуры:**
  - `rpc CreateCard (CreateCardRequest) returns (CardResponse)`
  - `rpc UpdateCard (UpdateCardRequest) returns (CardResponse)`
  - `rpc DeleteCard (DeleteCardRequest) returns (google.protobuf.Empty)`
- **Требование:** SR-VOC-01 / SR-VOC-04
- **Описание:** CRUD-операции над карточками. Карточка создается на основе заметки (`NotePayload`) и привязанного `NoteType`.

### CheckCardDuplicates
- **Сигнатура:** `rpc CheckCardDuplicates (CheckCardDuplicatesRequest) returns (CheckCardDuplicatesResponse)`
- **Требование:** SR-VOC-05
- **Описание:** Проверка наличия субординированных карточек с аналогичной точной формой/термином в проекте.

### CaptureCard
- **Сигнатура:** `rpc CaptureCard (CaptureCardRequest) returns (CardResponse)`
- **Требование:** SR-API-01 / SR-VOC-01
- **Описание:** Быстрое создание карточки из Chrome-расширения с сохранением контекстного предложения и медиа-ссылок.

### SearchCards / BulkCreateCards
- **Сигнатура:** `rpc SearchCards (SearchCardsRequest) returns (SearchCardsResponse)`
- **Сигнатура:** `rpc BulkCreateCards (BulkCreateCardsRequest) returns (BulkCreateCardsResponse)`
- **Требование:** SR-SRC-01 / SR-VOC-03
- **Описание:** Полнотекстовый поиск карточек по полям заметки и пакетный импорт (например, из CSV/Anki).

---

## 3. TermService (Управление Терминами)

### CreateOrUpdateTerm / MarkTermKnown / IgnoreTerm
- **Сигнатуры:**
  - `rpc CreateOrUpdateTerm (CreateOrUpdateTermRequest) returns (TermDetailsResponse)`
  - `rpc MarkTermKnown (TermActionRequest) returns (TermDetailsResponse)`
  - `rpc IgnoreTerm (TermActionRequest) returns (TermDetailsResponse)`
- **Требование:** SR-VOC-05
- **Описание:** Создание и перевод статуса термина точной формы (`ProjectTerm`) в состояния `SAVED`, `KNOWN` или `IGNORED`.

### BulkMarkKnown / ListProjectTerms
- **Сигнатуры:**
  - `rpc BulkMarkKnown (BulkMarkKnownRequest) returns (BulkMarkKnownResponse)`
  - `rpc ListProjectTerms (ListProjectTermsRequest) returns (ListProjectTermsResponse)`
- **Требование:** SR-VOC-05
- **Описание:** Массовая пометка терминов выученными (например, при перелистывании страницы в ридере) и листинг терминов проекта с пагинацией по курсору.
