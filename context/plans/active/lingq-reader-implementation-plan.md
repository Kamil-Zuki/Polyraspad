# План реализации LingQ-подхода для Library, Reader и карточек

Дата: 2026-05-02

## Цель

Перевести обучение в Library/Reader/Card creation с лемматизированной модели на модель реальных форм и фраз, как в LingQ:

- новые слова подсвечиваются синим;
- сохранённые слова/фразы становятся жёлтыми;
- известные слова становятся белыми;
- перевод сохраняется вместе с контекстом;
- оставшиеся синие слова могут становиться Known при перелистывании;
- пользователь может создавать phrase LingQ из нескольких соседних слов;
- review доступен прямо из reader;
- `sleep`, `slept`, `sleeping`, `went`, `go` считаются разными учебными единицами.

Публичные источники по поведению LingQ:

- https://lingq-support.groovehq.com/help/how-do-i-create-lingqs
- https://lingq-support.groovehq.com/help/how-do-i-increase-my-known-words
- https://www.lingq.com/en/ios-app-support/

## Принципиальное изменение

Сейчас система использует лемму как основу знания:

- `reader` отображает и использует `token.lemma`;
- backend мапит статусы через `ProjectLemmas`;
- карточки связаны с `Card.LemmaId`;
- дубликаты ищутся по лемме;
- статистика считает `totalLemmas`, `matureLemmas`, `learningLemmas`.

Новая модель:

- основной ключ знания: `termText`, нормализованная реальная форма из текста;
- для фраз: `termText` хранит всю выбранную фразу;
- лемма может остаться только как необязательная справочная подсказка для AI/словарей;
- статусы, дубликаты, карточки, reader-подсветка и статистика работают по real term, а не по лемме.

## Этап 1. Термины и статусы вместо лемм

### Что делаем

Вводим новую модель данных:

- `ProjectTerm`
  - `Id`
  - `ProjectId`
  - `Text`
  - `NormalizedText`
  - `Type`: `WORD` или `PHRASE`
  - `Language`
  - `CreatedAt`
  - `UpdatedAt`

- `UserTermStatus`
  - `Id`
  - `UserId`
  - `ProjectId`
  - `ProjectTermId`
  - `Status`: `NEW`, `LINGQ`, `KNOWN`, `IGNORED`
  - `Meaning`
  - `FirstSentence`
  - `FirstSourceTitle`
  - `FirstSourceUrl`
  - `LastSeenAt`
  - `CreatedAt`
  - `UpdatedAt`

- опционально `TermOccurrence`
  - хранит контексты, где термин встречался;
  - пригодится для "multiple contexts", истории чтения и аналитики.

### Важные правила

- `NormalizedText` строится из реальной формы: trim, lower-case, нормализация пробелов.
- `sleep` и `slept` имеют разные `ProjectTerm`.
- `went` и `go` имеют разные `ProjectTerm`.
- фраза `take off` имеет отдельный `ProjectTerm` и не заменяется на слова `take` + `off`.
- `ProjectLemma` и `Card.LemmaId` пока не удаляем, а переводим в legacy.

### Критерии готовности

- есть миграции;
- можно создать term/status без карточки;
- есть сервис для поиска статусов по списку `termText`;
- старые леммы не участвуют в новом reader flow.

## Этап 2. AnalyzeText без лемматизации как основы статусов

### Что делаем

Меняем `VocabularyService.Services.TextService`:

- токенизация возвращает `Text`, `Type`, `Status`, `TermText`;
- `Lemma` больше не нужен для определения статуса;
- статусы берутся из `ProjectTerm` + `UserTermStatus`;
- stop words можно автоматически отдавать как `KNOWN` или `IGNORED`, но только по реальной форме;
- статистика текста считает уникальные реальные формы.

Меняем fallback в `polyraspad-frontend/src/app/reader/reader-utils.ts`:

- `clientSideTokenize` больше не создаёт `lemma`;
- unique words считаются по `token.text.toLowerCase()`.

### Критерии готовности

- в ответе `/text/analyze` `slept` и `sleep` имеют независимые статусы;
- reader не показывает `/lemma/`;
- known percentage считается по real terms;
- существующие PDF/text flows продолжают открываться.

## Этап 3. API для LingQ-действий

### Что делаем

Добавляем backend endpoints/gRPC для действий reader:

- `CreateOrUpdateTerm`
  - создать жёлтый LingQ из слова или фразы;
  - сохранить значение и первый контекст;
  - опционально создать карточку.

- `MarkTermKnown`
  - перевести real term в `KNOWN`.

- `IgnoreTerm`
  - перевести term в `IGNORED`.

- `BulkMarkKnown`
  - используется при перелистывании страницы.

- `GetTermDetails`
  - meaning;
  - contexts;
  - связанные карточки;
  - текущий статус.

- `SearchTermDuplicates`
  - точное совпадение по `NormalizedText`;
  - похожие формы можно показывать отдельно, но они не блокируют создание.

### Критерии готовности

- клик по слову может менять статус без создания карточки;
- создание LingQ и создание карточки могут быть раздельными действиями;
- дубликаты ищутся по точной форме/фразе, не по лемме.

## Этап 4. Reader как основной учебный экран

### Что делаем

Перерабатываем `polyraspad-frontend/src/app/reader/page.tsx`:

- убрать отображение `word.lemma`;
- заменить `Lemma duplicates` на `Existing term cards` или `Existing LingQs`;
- клик по синему слову открывает инспектор:
  - выбранный term;
  - предложение;
  - быстрые переводы/AI translation;
  - поле своего значения;
  - кнопки `Create LingQ`, `Known`, `Ignore`;
  - кнопка создания карточки;
- клик по жёлтому слову открывает сохранённое значение, контексты и status controls;
- клик по белому слову позволяет вернуть слово в `LINGQ` или `NEW`;
- добавить счётчики страницы:
  - new terms;
  - LingQs;
  - known percentage;
  - review count.

### Reader layout

Целевой UX:

- левая зона: Library/lesson navigation, коллекции, главы/страницы;
- центр: чистый текст с нормальной типографикой и пагинацией;
- правая зона: inspector выбранного слова/фразы;
- верхняя или нижняя панель: page navigation, progress, review button, reader settings.

### Критерии готовности

- reader можно использовать без открытия editor;
- слово можно изучать прямо из текста;
- цветовая модель: blue/new, yellow/lingq, white/known, muted/ignored;
- UI не требует знания термина "lemma".

## Этап 5. Phrase LingQ

### Что делаем

Добавляем выбор нескольких соседних слов:

- drag/selection или shift-click;
- максимум фразы задать настройкой, например 8 слов;
- выбранная фраза отправляется как `termText`;
- phrase LingQ получает статус `LINGQ`;
- фразы подсвечиваются отдельно, например жёлто-оранжевым;
- при рендере фраза имеет приоритет над одиночными словами.

### Важное правило рендера

Если есть сохранённая фраза `take off`, reader подсвечивает `take off` как фразу, а не два отдельных слова, когда токены идут подряд.

### Критерии готовности

- можно создать phrase LingQ из reader;
- phrase сохраняется с переводом и контекстом;
- phrase попадает в review;
- phrase не ломает подсветку одиночных слов.

## Этап 6. Перелистывание как учебное действие

### Что делаем

Добавляем reader setting:

- `Mark remaining blue words as known on page turn`.

Поведение:

- если setting включён, при переходе на следующую страницу все оставшиеся `NEW` terms на текущей странице становятся `KNOWN`;
- если выключен, они остаются `NEW`;
- работает и для обычного текста, и для PDF pages;
- действие должно быть bulk endpoint, чтобы не стрелять запросом на каждое слово.

### Критерии готовности

- setting сохраняется для пользователя;
- page turn корректно обновляет статусы;
- пользователь может выключить автоматическое known-поведение.

## Этап 7. Карточки по реальной форме

### Что делаем

Меняем `CreateCard`, `CaptureCard`, `BulkCreateCards`, editor и reader card creation:

- `targetWord` всегда точная форма или фраза из предложения;
- backend больше не вызывает `ResolveForCardAsync` для создания/поиска леммы как основы карточки;
- `Card.LemmaId` не заполняется для новых карточек;
- карточка связывается с `ProjectTermId` или хранит связь через `targetWord` + `ProjectId`;
- создание карточки из reader должно обновлять term status до `LINGQ`, если он ещё `NEW`.

### Дубликаты

Новая логика:

- `slept` не дубль `sleep`;
- `went` не дубль `go`;
- `take off` не дубль `take`;
- точный дубль: такой же `NormalizedText` в этом проекте.

### Критерии готовности

- editor сохраняет точную форму;
- capture extension сохраняет точную форму;
- bulk import не лемматизирует target;
- duplicate UI больше не говорит "lemma duplicates".

## Этап 8. Library как content library

### Что делаем

Сдвигаем `/library` от deck library к библиотеке уроков/текстов:

- `Continue Reading`;
- импортированные тексты;
- PDF/text lessons;
- коллекции;
- прогресс чтения;
- количество new terms в уроке;
- количество LingQs;
- known percentage;
- быстрый вход в reader.

Decks можно оставить отдельной секцией, но основной UX Library должен вести к чтению, а не к управлению карточками.

### Критерии готовности

- пользователь видит, что дальше читать;
- видит прогресс по каждому тексту;
- может быстро импортировать новый текст/file/url;
- decks не мешают reading workflow.

## Этап 9. Статистика terms вместо lemmas

### Что делаем

Заменяем UI и DTO поля:

- `totalLemmas` -> `totalTerms`;
- `matureLemmas` -> `knownTerms` или `matureTerms`;
- `learningLemmas` -> `learningTerms`;
- добавить `newTerms`;
- добавить `knownPercent`;
- добавить `knownPhrases`, если phrase LingQ делаем отдельной метрикой.

Обновляем:

- `ProjectStatsBanner`;
- `/library`;
- `/analytics`;
- `/projects`;
- API DTO;
- backend analytics.

### Критерии готовности

- в UI нет "lemmas" как пользовательской метрики;
- статистика совпадает с real forms;
- старые поля поддерживаются временно только для обратной совместимости.

## Этап 10. Sentence View

### Что делаем

Добавляем режим одного предложения:

- next/prev sentence;
- перевод предложения;
- список terms в предложении;
- быстрые действия над словом/фразой;
- создание карточки из sentence view;
- возврат в full page reader.

### Критерии готовности

- пользователь может читать предложение за предложением;
- sentence translation не заменяет term translation;
- все term actions работают так же, как в full reader.

## Этап 11. Review из reader

### Что делаем

Добавляем кнопку `Review` прямо в reader:

- показывает количество LingQs/cards из текущего lesson;
- запускает review по yellow terms и phrase terms;
- SRS остаётся, но источник review - reader terms;
- карточки сохраняют контекст первого появления.

### Критерии готовности

- можно создать несколько LingQs и сразу открыть review;
- review не требует перехода в отдельную deck library;
- phrase LingQs попадают в review.

## Этап 12. Миграция legacy lemma data

### Что делаем

Переход без резкого удаления:

- для каждой существующей карточки создать `ProjectTerm` по `Card.TargetWord`;
- связать карточку с новым term;
- если у карточки есть progress, создать `UserTermStatus`;
- `Card.LemmaId`, `ProjectLemma`, `totalLemmas` оставить как legacy на период миграции;
- новые карточки больше не получают `LemmaId`;
- legacy поля постепенно убрать из UI, затем из API, затем из БД отдельной миграцией.

### Критерии готовности

- старые карточки открываются;
- старый прогресс не теряется;
- новые reader/card flows не используют леммы.

## Этап 13. Тестирование

### Backend tests

- `slept` и `sleep` получают разные статусы;
- `went` и `go` не считаются дублями;
- `CreateCard` сохраняет точный `targetWord`;
- `CaptureCard` сохраняет точный `targetWord`;
- `BulkCreateCards` не лемматизирует target;
- `BulkMarkKnown` обновляет только terms текущей страницы;
- `ProjectTerm` уникален по `ProjectId + NormalizedText + Type`.

### Frontend tests

- reader не отображает `/lemma/`;
- blue term -> create LingQ -> yellow;
- yellow term показывает meaning и context;
- white term можно вернуть в learning;
- phrase selection создаёт phrase term;
- phrase highlight имеет приоритет над word highlight;
- page turn переводит blue terms в known только при включённой настройке;
- duplicate panel показывает exact term matches.

### E2E smoke

- импорт текста;
- открыть reader;
- создать LingQ;
- создать phrase LingQ;
- перелистнуть страницу;
- создать карточку;
- открыть review из reader;
- проверить Library stats.

## Предлагаемый порядок реализации

1. Data model: `ProjectTerm`, `UserTermStatus`, миграции.
2. Term service и API для статусов.
3. `TextService.AnalyzeTextAsync` на real terms.
4. Frontend reader без lemma UI.
5. Reader actions: create LingQ, known, ignore.
6. Exact duplicate logic.
7. Card creation без `LemmaId`.
8. Page turn bulk known.
9. Phrase LingQ.
10. Library content-first layout.
11. Terms-based stats.
12. Sentence View.
13. Review from reader.
14. Legacy migration cleanup.

## Основные риски

- Существующие analytics и study flows могут ожидать `LemmaId`.
- Старые карточки нужно аккуратно мигрировать, чтобы не потерять progress.
- Подсветка phrase terms сложнее одиночных слов: нужен устойчивый алгоритм поиска фраз по токенам.
- PDF reader должен обновлять статусы без повторного тяжёлого анализа каждой страницы.
- UI Library сейчас deck-first, поэтому content-first изменение лучше делать отдельным этапом после стабилизации reader semantics.

## Definition of Done

Фича считается завершённой, когда:

- пользователь читает текст и видит blue/yellow/white модель без лемм;
- сохранённая форма появляется жёлтой в будущих текстах именно в этой форме;
- карточки создаются по реальной форме или фразе;
- дубликаты работают по точному термину;
- Library показывает прогресс по terms, а не lemmas;
- review можно запустить из reader;
- старые карточки доступны после миграции;
- тесты покрывают различие `sleep`/`slept` и `go`/`went`.
