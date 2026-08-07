# REST API Aggregator BFF: Проекты, Колоды, Карточки и Термины

Данный документ описывает REST-эндпоинты **AggregatorService**, проксирующие операции управления проектами, колодами, карточками и терминами в **VocabularyService**.

---

## 1. Управление Проектами (`/api/v1/projects`)

| Метод | Эндпоинт | gRPC метод | Описание |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/v1/projects` | `ContentService.GetProjects` | Список проектов текущего пользователя |
| `POST` | `/api/v1/projects` | `ContentService.CreateProject` | Создание нового языкового проекта |
| `GET` | `/api/v1/projects/{id}` | `ContentService.GetProjectDetails` | Детали проекта и параметры FSRS |
| `PUT` | `/api/v1/projects/{id}` | `ContentService.UpdateProject` | Обновление настроек проекта |

---

## 2. Управление Колодами (`/api/v1/decks`)

| Метод | Эндпоинт | gRPC метод | Описание |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/v1/decks/tree` | `ContentService.GetDeckTree` | Дерево колод с подсчетом карточек |
| `GET` | `/api/v1/decks/{id}` | `ContentService.GetDeckDetail` | Подробная статистика и настройки колоды |
| `POST` | `/api/v1/decks` | `ContentService.CreateDeck` | Создание пользовательской колоды |
| `PUT` | `/api/v1/decks/{id}` | `ContentService.UpdateDeck` | Редактирование колоды |
| `DELETE` | `/api/v1/decks/{id}` | `ContentService.DeleteDeck` | Удаление колоды |

---

## 3. Управление Карточками (`/api/v1/cards`)

| Метод | Эндпоинт | gRPC метод | Описание |
| :---: | :--- | :--- | :--- |
| `POST` | `/api/v1/cards` | `CardService.CreateCard` | Создание карточки с заметкой |
| `POST` | `/api/v1/cards/capture` | `CardService.CaptureCard` | Захваченная карточка из расширения |
| `GET` | `/api/v1/cards/search` | `CardService.SearchCards` | Полнотекстовый поиск карточек |
| `POST` | `/api/v1/cards/check-duplicates` | `CardService.CheckCardDuplicates` | Проверка существующих карточек по термину |
| `PUT` | `/api/v1/cards/{id}` | `CardService.UpdateCard` | Обновление карточки |
| `DELETE` | `/api/v1/cards/{id}` | `CardService.DeleteCard` | Удаление карточки |

---

## 4. Термины и Статусы (`/api/v1/terms`)

| Метод | Эндпоинт | gRPC метод | Описание |
| :---: | :--- | :--- | :--- |
| `POST` | `/api/v1/terms/mark-known` | `TermService.MarkTermKnown` | Пометка точной формы выученной |
| `POST` | `/api/v1/terms/ignore` | `TermService.IgnoreTerm` | Перевод термина в статус `IGNORED` |
| `POST` | `/api/v1/terms/bulk-mark-known` | `TermService.BulkMarkKnown` | Пакетная пометка (листание страниц) |
| `GET` | `/api/v1/terms` | `TermService.ListProjectTerms` | Список терминов проекта с пагинацией |
