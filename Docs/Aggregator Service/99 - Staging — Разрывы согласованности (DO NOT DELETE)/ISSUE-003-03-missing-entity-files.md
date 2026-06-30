# ISSUE-003: В индексе `03` указаны entity-файлы, которых нет на диске

## Тип

Пробел

## В двух словах

В `Entities - Список сущностей` перечислены пять групп контрактных сущностей с wikilinks, но на диске существует только `Entity - Reader и термины - Reader.md`. SR в `01` для Auth, Content, Cards, Media, Community и Billing не имеют соответствующих entity-документов в `03`.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 03 | `Entities - Список сущностей` → `Entity - Аутентификация и профиль - Auth Proxy` | Файл отсутствует |
| 03 | `Entities - Список сущностей` → `Entity - Проекты и колоды - Content` | Файл отсутствует |
| 03 | `Entities - Список сущностей` → `Entity - Карточки и обучение - Cards Study` | Файл отсутствует |
| 03 | `Entities - Список сущностей` → `Entity - Медиа и библиотека - Media` | Файл отсутствует |
| 03 | `Entities - Список сущностей` → `Entity - Сообщество и биллинг - Community Billing` | Файл отсутствует |
| 01 | SR-AGG-AUTH-01..08, SR-AGG-CONTENT-01..03, … | Нет boundary-entity в `03` кроме Reader |

Путь к файлу (вторично): `03 - Модель Данных/01 - Основные сущности/Entities - Список сущностей микросервиса Aggregator Service.md`

## Доказательство

Индекс ссылается:

```markdown
| Auth API | [[Entity - Аутентификация и профиль - Auth Proxy]] | authorization-module |
| Content | [[Entity - Проекты и колоды - Content]] | VocabularyService.ContentService |
```

В каталоге `03/01 - Основные сущности/` только два файла: index + `Entity - Reader и термины - Reader.md`.

## Рекомендуемое действие

1. Создать недостающие entity-файлы как **контракты границы BFF** (JSON DTO shapes), не PostgreSQL tables.
2. Либо временно убрать broken wikilinks из index до создания файлов.
3. Не править `01` SR — они корректны для BFF; закрыть ISSUE после появления entity в `03`.

## Статус

Open
