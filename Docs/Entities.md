---
title: "PVS — схема базы данных"
aliases: ["Entities", "PVS-DB-Schema"]
tags: [polyraspad, docs, database, postgresql]
doc_id: PVS-DB-Schema-2025-1
---

# Personal Vocabulary Service

## Описание схемы базы данных

| Параметр      | Значение             |
| ------------- | -------------------- |
| Код документа | PVS-DB-Schema-2025-1 |
| Версия        | 1.0                  |
| СУБД          | PostgreSQL 15+       |
| Дата          | 03/12/2025           |
| Автор         | Каратов К.А.         |
| Утверждено    | —                    |

---

## Введение

Настоящий документ содержит детальное техническое описание схемы базы данных (схема internal) для микросервиса **Personal Vocabulary Service (PVS)**.

База данных спроектирована в соответствии со стандартом **Steos-DB-WhiteBook** и обеспечивает реализацию функциональных требований версии **7.0 (Golden Master)**. Архитектура хранилища адаптирована под иерархическую структуру проектов, методику Sentence Mining, работу в офлайн-режиме и поддержку маркетплейса контента.

### Ключевые принципы проектирования

1.  **Изоляция сервиса:** База данных принадлежит исключительно микросервису PVS. Прямой доступ из других сервисов запрещен.

2.  **Именование:** Все объекты (таблицы, колонки, индексы) именуются в snake_case.

3.  **Типизация:**

    - Идентификаторы: uuid (UUID v4).

    - Время: timestamptz (хранение в UTC).

    - Сложные структуры: jsonb (для метаданных, настроек и разметки текста).

4.  **Целостность:** Использование внешних ключей (Foreign Keys) с каскадным удалением (ON DELETE CASCADE) для поддержки требований GDPR (SR-BG-03).

5.  **Производительность:** Использование GIN-индексов для полнотекстового поиска и JSONB-запросов.

## Содержание
### 1. Ядро: Контент и Иерархия (Content Core)

Таблицы, отвечающие за хранение учебных материалов и их организацию.

- **1.1. projects** --- Корневая сущность (Языковые курсы).

- **1.2. decks** --- Тематические колоды и папки.

- **1.3. cards** --- Фразовые карточки (Sentence Mining).

- **1.4. deck_versions** --- История версий колод (Снэпшоты).

### 2. Движок Обучения и Лингвистика (Learning & NLP)

Таблицы, отвечающие за алгоритмы FSRS, прогресс пользователя и словарный запас.

- **2.1. user_card_progress** --- Состояние SRS (Stability, Difficulty, Due).

- **2.2. project_lemmas** --- Глобальный словарь лемм.

- **2.3. study_sessions** --- Журнал завершенных уроков.

- **2.4. review_logs** --- Детальный лог ответов.

### 3. Маркетплейс и Права (Marketplace & Entitlements)

Таблицы для реализации экономики авторов и защиты контента.

- **3.1. products** --- Товарная упаковка колод.

- **3.2. user_entitlements** --- Реестр прав доступа.

- **3.3. product_reviews** --- Отзывы и рейтинги.

### 4. Коллаборация (Community)

Таблицы для совместной работы и социальных механик.

- **4.1. contributions** --- Предложения изменений (Pull Requests).

- **4.2. deck_subscriptions** --- Подписки на публичные колоды.

- **4.3. author_profiles** --- Публичные профили авторов.

### 5. Системные и Служебные (System & Sync)

Таблицы для синхронизации и настроек.

- **5.1. user_settings** --- Глобальные настройки пользователя.

- **5.2. deleted_objects** --- \"Надгробия\" (Tombstones) для Delta Sync.

---

## 1. Ядро: Контент и Иерархия (Content Core)

Таблицы в схеме internal, отвечающие за хранение пользовательских данных и их структуру.

### 1.1. Таблица projects (Проекты)

Корневая сущность. Реализует требования **SR-STR-01** и **SR-STR-02**.\
Проект служит жестким разделителем: карточки, настройки алгоритмов и статистика одного проекта никак не влияют на другой.

**DDL:**


CREATE TABLE internal.projects (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

title text NOT NULL,

source_lang varchar(5) NOT NULL,

target_lang varchar(5) NOT NULL,

fsrs_settings jsonb NOT NULL DEFAULT \'{\"request_retention\": 0.9, \"maximum_interval\": 36500, \"w\": []}\'::jsonb,

stats jsonb NOT NULL DEFAULT \'{\"total_lemmas\": 0, \"mature_lemmas\": 0}\'::jsonb,

is_archived boolean NOT NULL DEFAULT false,

created_at timestamptz NOT NULL DEFAULT now(),

updated_at timestamptz NOT NULL DEFAULT now()

);

\-- Индекс для получения списка проектов пользователя (Дашборд)

CREATE INDEX idx_projects_user_id ON internal.projects (user_id);

### 1.2. Таблица decks (Колоды)

Тематические контейнеры. Реализует **SR-STR-03** (иерархия) и **SR-PUB-02** (клонирование).\
Колоды могут быть вложенными (папки). Удаление родительской колоды каскадно удаляет все дочерние (или переносит их --- зависит от бизнес-логики, здесь выбран каскад для чистоты структуры).

**DDL:**




CREATE TABLE internal.decks (

id uuid PRIMARY KEY,

project_id uuid NOT NULL,

parent_deck_id uuid,

owner_id uuid NOT NULL,

title text NOT NULL,

description text,

cover_image_url text,

is_public boolean NOT NULL DEFAULT false,

contribution_policy varchar(20) NOT NULL DEFAULT \'OPEN\',

license_type varchar(20) NOT NULL DEFAULT \'PRIVATE\',

forked_from_id uuid,

card_count integer NOT NULL DEFAULT 0,

created_at timestamptz NOT NULL DEFAULT now(),

updated_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_decks_projects FOREIGN KEY (project_id)

REFERENCES internal.projects(id) ON DELETE CASCADE,

CONSTRAINT fk_decks_parent FOREIGN KEY (parent_deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE

);

CREATE INDEX idx_decks_project_id ON internal.decks (project_id);

CREATE INDEX idx_decks_parent_deck_id ON internal.decks (parent_deck_id);

\-- Индекс для фильтрации публичных колод

CREATE INDEX idx_decks_public ON internal.decks (is_public) WHERE is_public = true;

### 1.3. Таблица cards (Карточки)

Единица обучения. Структура полностью переработана под **Sentence Mining** (SR-VOC-01).\
Вместо Front/Back мы храним предложение и метаданные о целевом слове.

  ---------------
  Колонка
  ---------------
  id

  deck_id

  creator_id

  sentence

  translation

  target_word

  target_index

  source_meta

  media

  lemma_id

  external_id

  search_vector

  created_at

  updated_at
  ---------------

**DDL:**




CREATE TABLE internal.cards (

id uuid PRIMARY KEY,

deck_id uuid NOT NULL,

creator_id uuid NOT NULL,

sentence text NOT NULL,

translation text NOT NULL,

target_word text NOT NULL,

target_index jsonb NOT NULL DEFAULT \'{}\'::jsonb,

source_meta jsonb,

media jsonb,

synonyms jsonb,

lemma_id uuid, \-- FK добавим после создания таблицы project_lemmas

external_id text,

\-- Полнотекстовый поиск (автогенерируемая колонка)

search_vector tsvector GENERATED ALWAYS AS (to_tsvector(\'english\', sentence)) STORED,

created_at timestamptz NOT NULL DEFAULT now(),

updated_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_cards_decks FOREIGN KEY (deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE

);

CREATE INDEX idx_cards_deck_id ON internal.cards (deck_id);

\-- GIN индекс для быстрого поиска

CREATE INDEX idx_cards_search ON internal.cards USING GIN (search_vector);

### 1.4. Таблица deck_versions (История версий)

Необходима для реализации **SR-MOD-03** (откат при бане) и **SR-VOC-07** (синхронизация обновлений). Хранит \"снимки\" состояния колоды.

**DDL:**




CREATE TABLE internal.deck_versions (

id uuid PRIMARY KEY,

deck_id uuid NOT NULL,

version_number int4 NOT NULL,

change_description text NOT NULL,

modified_by_user_id uuid NOT NULL,

snapshot_ref text NOT NULL, \-- Ссылка на S3

created_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_deck_versions_decks FOREIGN KEY (deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE

);

CREATE INDEX idx_deck_versions_deck_id ON internal.deck_versions (deck_id);

## Раздел 2. Движок Обучения и Лингвистика (Learning & NLP)
### 2.1. Таблица user_card_progress (Прогресс FSRS)

Хранит состояние памяти пользователя для каждой карточки. Реализует требования **SR-LRN-03** (FSRS) и **SR-LRN-05** (Leeches).

- **Денормализация:** Поле project_id добавлено намеренно, чтобы при генерации очереди (SR-LRN-01) не делать тяжелый JOIN с таблицами cards -\> decks -\> projects.

- **FSRS v5:** Используются поля stability и difficulty вместо старого ease_factor (SM-2).

**DDL:**




CREATE TABLE internal.user_card_progress (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

card_id uuid NOT NULL,

project_id uuid NOT NULL, \-- Денормализация

state int2 NOT NULL DEFAULT 0,

step int4 NOT NULL DEFAULT 0,

stability float4 NOT NULL DEFAULT 0,

difficulty float4 NOT NULL DEFAULT 0,

due timestamptz NOT NULL,

elapsed_days int4 NOT NULL DEFAULT 0,

scheduled_days int4 NOT NULL DEFAULT 0,

reps int4 NOT NULL DEFAULT 0,

lapses int4 NOT NULL DEFAULT 0,

is_suspended boolean NOT NULL DEFAULT false,

last_review timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_progress_cards FOREIGN KEY (card_id)

REFERENCES internal.cards(id) ON DELETE CASCADE,

CONSTRAINT fk_progress_projects FOREIGN KEY (project_id)

REFERENCES internal.projects(id) ON DELETE CASCADE

);

\-- Критически важный индекс для генерации очереди (SR-PERF-01)

\-- Позволяет мгновенно выбрать карты \"На сегодня\" для конкретного юзера и проекта

CREATE INDEX idx_progress_queue_gen

ON internal.user_card_progress (user_id, project_id, state, due)

WHERE is_suspended = false;

CREATE INDEX idx_progress_card_id ON internal.user_card_progress (card_id);

### 2.2. Таблица project_lemmas (Словарь лемм)

Глобальный реестр слов в рамках проекта. Реализует **SR-TXT-03** и **SR-ANL-01**.\
Связывает разрозненные карточки (например, с формами \"go\", \"went\") в единую сущность для подсчета словарного запаса.

**DDL:**




CREATE TABLE internal.project_lemmas (

id uuid PRIMARY KEY,

project_id uuid NOT NULL,

text text NOT NULL,

pos_tag varchar(10),

status varchar(20) NOT NULL DEFAULT \'NEW\',

main_card_id uuid,

updated_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_lemmas_projects FOREIGN KEY (project_id)

REFERENCES internal.projects(id) ON DELETE CASCADE,

CONSTRAINT uq_project_lemma UNIQUE (project_id, text, pos_tag)

);

\-- Индекс для режима \"Читалка\" (быстрый поиск статусов слов из текста)

CREATE INDEX idx_lemmas_text ON internal.project_lemmas (project_id, text);

> **Примечание:** После создания этой таблицы, нужно добавить FK в таблицу cards (поле lemma_id), которое мы описали в Разделе 1.
>
> code SQL
>
> downloadcontent_copy
>
> expand_less

ALTER TABLE internal.cards

ADD CONSTRAINT fk_cards_lemmas FOREIGN KEY (lemma_id)

REFERENCES internal.project_lemmas(id) ON DELETE SET NULL;

### 2.3. Таблица study_sessions (Сессии)

Журнал сессий обучения (статусы в т.ч. **ACTIVE** и завершённые). Используется для построения **Heatmap** (SR-ANL-02), истории активности и привязки **review_logs**. Очередь карточек текущей сессии хранится в **Redis** (см. `StudyService`), не в этой таблице.

**DDL:**




CREATE TABLE internal.study_sessions (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

project_id uuid NOT NULL,

deck_id uuid, \-- Nullable, если учил весь проект (SR-STR-03)

start_time timestamptz NOT NULL,

end_time timestamptz NOT NULL,

cards_reviewed int4 NOT NULL DEFAULT 0,

duration_sec int4 NOT NULL DEFAULT 0,

new_learned int4 NOT NULL DEFAULT 0,

status varchar(20) NOT NULL DEFAULT 'ACTIVE',

CONSTRAINT fk_sessions_projects FOREIGN KEY (project_id)

REFERENCES internal.projects(id) ON DELETE CASCADE

);

\-- Индекс для построения Heatmap за год

CREATE INDEX idx_sessions_heatmap ON internal.study_sessions (user_id, project_id, end_time);

### 2.4. Таблица review_logs (Лог ответов)

Неизменяемый журнал («Append-only log») каждого действия пользователя.\
Необходим для:

1.  Функции **Undo** (SR-LRN-08) --- откат последнего действия.

2.  Подсчета **Streaks** (SR-ANL-03) --- дневная сводка собирается агрегацией этих логов.

3.  Оптимизации алгоритма FSRS (анализ истории для подстройки весов).

  -------------------------------------------------------------------------------------
  Колонка                  Тип           Описание
  ------------------------ ------------- --------------------------------------------------
  id                       uuid          **PK**.

  user_id                  uuid          Кто.

  card_id                  uuid          Какую карточку.

  session_id               uuid          В рамках какой сессии.

  rating                   int2          Оценка: 1 (Again), 2 (Hard), 3 (Good), 4 (Easy).

  state_before             int2          Состояние FSRS до ответа.

  state_after              int2          Состояние FSRS после ответа.

  due_before               timestamptz   Срок до.

  due_after                timestamptz   Срок после.

  review_duration_ms       int4          Время раздумий (в мс).

  user_answer              text          Опциональный текстовый ответ пользователя (для валидации).

  answer_validation_result jsonb         Результат проверки ответа (JSON, если user_answer был передан).

  created_at               timestamptz   Время события.
  -------------------------------------------------------------------------------------

**DDL:**




CREATE TABLE internal.review_logs (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

card_id uuid NOT NULL,

session_id uuid NOT NULL,

rating int2 NOT NULL,

state_before int2 NOT NULL,

state_after int2 NOT NULL,

due_before timestamptz NOT NULL,

due_after timestamptz NOT NULL,

stability_before float4 NOT NULL DEFAULT 0,

stability_after float4 NOT NULL DEFAULT 0,

difficulty_before float4 NOT NULL DEFAULT 0,

difficulty_after float4 NOT NULL DEFAULT 0,

review_duration_ms int4 NOT NULL,

user_answer text,

answer_validation_result jsonb,

created_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_logs_cards FOREIGN KEY (card_id)

REFERENCES internal.cards(id) ON DELETE CASCADE

);

\-- Индекс для Undo (поиск последнего действия в сессии)

CREATE INDEX idx_logs_session_created ON internal.review_logs (session_id, created_at DESC);

\-- Индекс для аналитики (сколько кард выучил сегодня)

CREATE INDEX idx_logs_user_date ON internal.review_logs (user_id, created_at);

Отлично. Переходим к третьему разделу.

Этот блок таблиц обеспечивает реализацию **Creator Economy**: позволяет авторам упаковывать колоды в товары, а системе --- контролировать доступ на основе покупок (интеграция с Billing Service).

## Раздел 3. Маркетплейс и Права (Marketplace & Entitlements)
### 3.1. Таблица products (Товары)

Обертка над колодой для продажи в Маркетплейсе. Реализует **SR-MKT-01**.\
Отделяет сущность \"Учебный материал\" (Колода) от сущности \"Товар на витрине\" (Цена, Маркетинг).

**DDL:**




CREATE TABLE internal.products (

id uuid PRIMARY KEY,

author_id uuid NOT NULL,

linked_deck_id uuid NOT NULL,

title text NOT NULL,

description_html text,

cover_image_url text,

price numeric(10, 2) NOT NULL DEFAULT 0.00,

currency char(3) NOT NULL DEFAULT \'USD\',

status varchar(20) NOT NULL DEFAULT \'DRAFT\',

average_rating float4 NOT NULL DEFAULT 0,

review_count int4 NOT NULL DEFAULT 0,

sales_count int4 NOT NULL DEFAULT 0,

created_at timestamptz NOT NULL DEFAULT now(),

updated_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_products_decks FOREIGN KEY (linked_deck_id)

REFERENCES internal.decks(id) ON DELETE RESTRICT

);

\-- Индекс для витрины (поиск опубликованных товаров)

CREATE INDEX idx_products_status ON internal.products (status) WHERE status = \'PUBLISHED\';

\-- Индекс для кабинета автора

CREATE INDEX idx_products_author ON internal.products (author_id);

### 3.2. Таблица user_entitlements (Права доступа)

Реестр прав. Реализует **SR-MKT-03** и **SR-COL-07**.\
Это \"билет\", который разрешает пользователю доступ к приватной или платной колоде.

**DDL:**




CREATE TABLE internal.user_entitlements (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

product_id uuid, \-- Может быть NULL, если доступ получен через Contribution

deck_id uuid NOT NULL,

source varchar(20) NOT NULL,

external_order_id text,

granted_at timestamptz NOT NULL DEFAULT now(),

is_active boolean NOT NULL DEFAULT true,

CONSTRAINT fk_entitlements_products FOREIGN KEY (product_id)

REFERENCES internal.products(id) ON DELETE SET NULL,

CONSTRAINT fk_entitlements_decks FOREIGN KEY (deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE

);

\-- Критически важный индекс для проверки доступа (Middleware)

CREATE INDEX idx_entitlements_check ON internal.user_entitlements (user_id, deck_id)

WHERE is_active = true;

### 3.3. Таблица product_reviews (Отзывы)

Реализует **SR-MKT-05**. Позволяет оставлять отзывы только при наличии записи в user_entitlements.

  ---------------------------------------------------------------------------------------
  Колонка        Тип           Описание
  -------------- ------------- ----------------------------------------------------------
  id             uuid          **PK**.

  product_id     uuid          **FK**. К какому товару.

  user_id        uuid          Автор отзыва.

  rating         int2          Оценка (1-5).

  comment        text          Текст отзыва.

  is_verified    bool          Флаг \"Verified Purchase\" (всегда true в нашей логике).

  author_reply   text          Ответ автора товара.

  created_at     timestamptz   Дата создания.
  ---------------------------------------------------------------------------------------

**DDL:**




CREATE TABLE internal.product_reviews (

id uuid PRIMARY KEY,

product_id uuid NOT NULL,

user_id uuid NOT NULL,

rating int2 NOT NULL CHECK (rating \>= 1 AND rating \<= 5),

comment text,

is_verified boolean NOT NULL DEFAULT true,

author_reply text,

created_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_reviews_products FOREIGN KEY (product_id)

REFERENCES internal.products(id) ON DELETE CASCADE

);

CREATE INDEX idx_reviews_product ON internal.product_reviews (product_id, created_at DESC);

Переходим к четвертому разделу.

Этот блок таблиц превращает сервис из «одиночной игры» в социальную платформу. Здесь реализуется **Git-like механика** предложений (Contributions) и система подписок.

## Раздел 4. Коллаборация (Community)
### 4.1. Таблица contributions (Предложения)

Аналог Pull Request. Реализует требования **SR-COL-01**, **SR-COL-02**, **SR-COL-03**.\
Хранит предложенные изменения в статусе \"Черновик\", не затрагивая основные данные до момента утверждения (Merge).

**DDL:**




CREATE TABLE internal.contributions (

id uuid PRIMARY KEY,

target_deck_id uuid NOT NULL,

target_card_id uuid, \-- Может ссылаться на cards, но ON DELETE SET NULL, чтобы сохранить историю вкладов даже если карту удалят

author_id uuid NOT NULL,

type varchar(10) NOT NULL,

payload jsonb NOT NULL,

comment text,

status varchar(20) NOT NULL DEFAULT \'PENDING\',

reviewer_id uuid,

resolution_comment text,

created_at timestamptz NOT NULL DEFAULT now(),

updated_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_contributions_decks FOREIGN KEY (target_deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE,

CONSTRAINT fk_contributions_cards FOREIGN KEY (target_card_id)

REFERENCES internal.cards(id) ON DELETE SET NULL

);

\-- Индекс для Модератора (Входящие предложения)

CREATE INDEX idx_contributions_pending ON internal.contributions (target_deck_id) WHERE status = \'PENDING\';

\-- Индекс для Автора (Мои вклады)

CREATE INDEX idx_contributions_author ON internal.contributions (author_id);

### 4.2. Таблица deck_subscriptions (Подписки)

Связь «Многие-ко-многим» между пользователями и публичными колодами.\
Реализует **SR-DECK-05** и **SR-SYNC-01** (для получения обновлений).

**DDL:**




CREATE TABLE internal.deck_subscriptions (

id uuid PRIMARY KEY,

user_id uuid NOT NULL,

deck_id uuid NOT NULL,

last_synced_version int4 DEFAULT 0,

subscribed_at timestamptz NOT NULL DEFAULT now(),

last_accessed_at timestamptz NOT NULL DEFAULT now(),

CONSTRAINT fk_subs_decks FOREIGN KEY (deck_id)

REFERENCES internal.decks(id) ON DELETE CASCADE,

\-- Уникальная подписка

CONSTRAINT uq_user_deck_sub UNIQUE (user_id, deck_id)

);

CREATE INDEX idx_subs_user ON internal.deck_subscriptions (user_id);

CREATE INDEX idx_subs_deck ON internal.deck_subscriptions (deck_id);

### 4.3. Таблица author_profiles (Профили авторов)

Публичная визитка автора в PVS. Реализует **SR-PUB-04**.\
Хранит данные, специфичные для образовательной платформы (бейджи, статистика), которые не хранятся в общем Identity Service.

  --------------
  Колонка
  --------------
  user_id

  display_name

  bio

  social_links

  badges

  stats_cache

  updated_at
  --------------

**DDL:**




CREATE TABLE internal.author_profiles (

user_id uuid PRIMARY KEY, \-- 1-to-1 с пользователем

display_name text,

bio text,

social_links jsonb DEFAULT \'{}\'::jsonb,

badges jsonb DEFAULT \'\[\]\'::jsonb,

stats_cache jsonb DEFAULT \'{}\'::jsonb,

updated_at timestamptz NOT NULL DEFAULT now()

);

Отлично. Переходим к финальному разделу.

Этот блок таблиц обеспечивает работу «невидимых» механизмов сервиса: синхронизации данных между устройствами (Delta Sync) и персонализации опыта.

## Раздел 5. Системные и Служебные (System & Sync)
### 5.1. Таблица user_settings (Настройки пользователя)

Хранит глобальные настройки профиля в рамках PVS. Также используется для кэширования агрегатов активности (Streak), чтобы не пересчитывать их при каждом запросе.\
Реализует требования **SR-ANL-03** (Streaks) и **SR-SETT-01** (Настройки).

**DDL:**




CREATE TABLE internal.user_settings (

user_id uuid PRIMARY KEY,

rollover_hour int4 NOT NULL DEFAULT 4,

current_streak int4 NOT NULL DEFAULT 0,

max_streak int4 NOT NULL DEFAULT 0,

last_study_date date,

daily_goal_new int4 NOT NULL DEFAULT 20,

daily_goal_review int4 NOT NULL DEFAULT 100,

interface_language varchar(5) NOT NULL DEFAULT \'en\',

updated_at timestamptz NOT NULL DEFAULT now()

);

### 5.2. Таблица deleted_objects (Надгробия / Tombstones)

Критически важна для **SR-SNC-01 (Delta Sync)**.\
Когда объект (карточка, колода) удаляется из БД физически (Hard Delete), мы должны оставить \"след\", чтобы клиентские приложения при следующей синхронизации узнали, что этот объект нужно удалить локально.

**DDL:**




CREATE TABLE internal.deleted_objects (

id uuid PRIMARY KEY,

entity_id uuid NOT NULL,

entity_type varchar(20) NOT NULL,

user_id uuid NOT NULL,

parent_id uuid, \-- Может быть NULL, если удален Проект

deleted_at timestamptz NOT NULL DEFAULT now()

);

\-- Индекс для Delta Sync: \"Дай мне все удаления пользователя X, произошедшие после времени Y\"

CREATE INDEX idx_deleted_sync ON internal.deleted_objects (user_id, deleted_at);

## Заключение

Схема базы данных спроектирована с учетом следующих требований:

1.  **Масштабируемость:** Разделение на проекты и использование uuid позволяет легко шардировать данные в будущем (например, по user_id или project_id).

2.  **Производительность:**

    - Для тяжелых выборок (очередь обучения) используются денормализованные поля в user_card_progress.

    - Для поиска используются GIN индексы.

    - Для списков используются кэшированные счетчики (card_count, stats).

3.  

4.  **Целостность:** Жесткие внешние ключи (ON DELETE CASCADE) гарантируют, что при удалении пользователя или проекта не останется \"мусора\", что соответствует требованиям GDPR.

5.  **Гибкость:** Использование JSONB для метаданных карточек и настроек позволяет добавлять новые фичи (например, новые типы медиа или параметры алгоритма) без изменения схемы таблиц (без ALTER TABLE).

Документ является финальной спецификацией для реализации слоя хранения данных (DAL).

**Документ PVS-DB-Schema-2025-1 полностью сформирован.**
