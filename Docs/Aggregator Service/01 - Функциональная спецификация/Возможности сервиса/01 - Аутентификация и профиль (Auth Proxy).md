# Группа 1: Аутентификация и профиль (Auth Proxy)

## Введение

В этом разделе описывается роль **Aggregator Service** как **прокси аутентификации и профиля** между клиентом Polyraspad и микросервисом **authorization-module**.

Aggregator **не хранит** учётные записи, пароли и refresh-токены. Он принимает REST-запросы от frontend, при необходимости применяет rate limiting, преобразует JSON DTO в gRPC-вызовы `Pvs.Auth.Grpc.AuthService` и возвращает клиенту HTTP-ответ с маппингом кодов ошибок downstream.

Для **защищённых** маршрутов (профиль, logout, смена пароля) Aggregator **локально валидирует JWT**, выданный authorization-module, извлекает `user_id` из claim `sub` и передаёт идентификатор в gRPC metadata. Повторная проверка credentials на каждый такой запрос в auth-сервис **не выполняется** — доверие строится на общем секрете подписи JWT (`Jwt:Secret`, `Issuer`, `Audience`).

**Метафора:**

Представьте **единую стойку регистрации** в офисном центре Polyraspad. Посетитель не ходит в HR-отдел (authorization-module) сам — он сдаёт документы на стойке (Aggregator). Стойка проверяет пропуск (JWT) у тех, кто уже внутри, и для новых гостей передаёт анкету в HR по внутренней связи (gRPC). HR хранит архив; стойка — только окно приёма и выдачи ответов.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/01 - Аутентификация и профиль (Auth)|REST API — Auth]].  
Архитектура JWT: [[02 - КАР-2 - Локальная валидация JWT]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к прокси аутентификации и профиля.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-AUTH-01** | **Регистрация нового пользователя:** Публичный REST-фасад создания учётной записи; credentials передаются в authorization-module и не сохраняются на шлюзе. |
| **SR-AGG-AUTH-02** | **Аутентификация и выдача JWT:** Единая точка входа — после проверки пароля клиент получает пару access и refresh token. |
| **SR-AGG-AUTH-03** | **Обновление access token:** Продление сессии по refresh token без повторного ввода пароля; endpoint публичный и защищён rate limit. |
| **SR-AGG-AUTH-04** | **Подтверждение адреса email:** Обработка ссылки из письма — передача одноразового token в auth-сервис для верификации. |
| **SR-AGG-AUTH-05** | **Профиль авторизованного пользователя:** Чтение актуальных атрибутов профиля; identity из JWT, данные запрашиваются у authorization-module. |
| **SR-AGG-AUTH-06** | **Выход из системы:** Завершение сессии и отзыв refresh token в auth-сервисе при наличии Bearer access. |
| **SR-AGG-AUTH-07** | **Изменение данных профиля:** Обновление username, password и avatar URL; идентификатор пользователя только из JWT, не из тела запроса. |
| **SR-AGG-AUTH-08** | **Ограничение частоты публичной auth:** Защита register, login, refresh и confirm-email — не более 10 запросов в минуту с одного IP до вызова downstream. |

---

# Детальная спецификация требований

## SR-AGG-AUTH-01: Регистрация пользователя {#SR-AGG-AUTH-01}

Новый пользователь Polyraspad должен иметь единую точку регистрации через публичный API. Aggregator не создаёт учётные записи сам — он принимает заявку от frontend и передаёт её в authorization-module, где выполняется проверка email и сохранение пароля.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Thin BFF** | Aggregator не создаёт записей в БД — только проксирует gRPC `RegisterUser`. |
| **Контракт REST** | Тело запроса: `UserRegistrationDto` (email, password, confirmPassword). |
| **Rate limit** | Endpoint под policy `auth-public` (см. SR-AGG-AUTH-08). |
| **Маппинг ошибок** | `InvalidArgument` → 400, `AlreadyExists` → 409, иначе → 502. |

### 2. Высокоуровневое описание

Представим регистрацию как **оформление абонемента в спортзале через стойку администратора**.

1. **Посетитель (Frontend):** заполняет анкету — email и пароль — и отдаёт её на стойку, не заходя в архив клиентов.
2. **Администратор (Aggregator):** проверяет, что форма заполнена, и сразу звонит в центральный офис (authorization-module), не заводя карточку локально.
3. **Центральный офис (authorization-module):** проверяет, свободен ли email, создаёт учётную запись и инициирует письмо подтверждения.
4. **Ответ посетителю:** администратор сообщает «анкета принята, подтвердите email» — **без выдачи пропуска (JWT)** на этом шаге.

Таким образом, Aggregator выступает **транспортом заявки**, а не владельцем identity. Политика подтверждения email и хранение пароля полностью на стороне auth-сервиса.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Frontend (страница регистрации).
* **Маршрут:** POST `/api/Auth/register`.
* **Downstream:** gRPC `RegisterUser`.

#### Сценарий А: Успешная регистрация (Happy Path)

**Сценарий:** Пользователь впервые регистрируется с уникальным email.

1. **Запрос (Frontend):** POST с `UserRegistrationDto` — email, password ≥ 6 символов, confirmPassword совпадает.
2. **Rate limit (BFF):** policy `auth-public` проверяет лимит по IP; запрос пропускается.
3. **Маппинг и gRPC (BFF):** DTO → `RegisterUserRequest`; вызов `RegisterUser` на authorization-module.
4. **Создание (Auth):** пользователь сохранён; SMTP отправляет confirm link.
5. **Ответ:** HTTP **201 Created**, тело `AuthResponseDto` с текстовым статусом.

#### Сценарий Б: Email уже занят (Negative Path)

1. **gRPC (Auth):** возвращает `AlreadyExists`.
2. **Маппинг (BFF):** HTTP **409 Conflict**, `{ "error": "<detail>" }`.

---

## SR-AGG-AUTH-02: Вход и выдача JWT {#SR-AGG-AUTH-02}

Login — точка, где клиент получает **пару токенов** для дальнейшей работы с API. Aggregator централизует выдачу JWT для всего Polyraspad frontend.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Единая точка login** | Все клиенты Polyraspad получают JWT через Aggregator, не напрямую из auth-module. |
| **Token pair** | Ответ содержит `accessToken` и `refreshToken` (`TokenResponseDto`). |
| **Последующие запросы** | Access token передаётся в `Authorization: Bearer …`; Aggregator валидирует локально. |

### 2. Высокоуровневое описание

Представим вход как **получение временного пропуска в здание**.

1. **Сотрудник (Пользователь):** называет email и пароль на стойке — это единственный момент, когда секрет передаётся по сети на login endpoint.
2. **Охранник (Aggregator):** не проверяет пароль сам — передаёт credentials в HR-систему (`LoginUser`).
3. **HR-система (authorization-module):** сверяет hash пароля и подписывает **access JWT** и **refresh token**.
4. **Выдача пропуска:** Aggregator возвращает оба токена клиенту; frontend сохраняет их (localStorage / memory). **HttpOnly Cookie на BFF не устанавливается** — в Polyraspad используется Bearer JWT, а не Phantom Token pattern.

Таким образом, после login Aggregator **не участвует** в проверке пароля на каждый запрос — только в локальной валидации подписи access token.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/Auth/login`, body `UserLoginDto`.
* **Downstream:** gRPC `LoginUser`.

#### Сценарий А: Успешный вход (Happy Path)

1. **Login (Frontend):** POST с email и password.
2. **gRPC (BFF → Auth):** `LoginUser` → `TokenResponseDto` mapped.
3. **Ответ:** HTTP **200**, `{ accessToken, refreshToken }`.
4. **Дальнейшие запросы:** Frontend добавляет `Authorization: Bearer {accessToken}` ко всем `/api/*`.

#### Сценарий Б: Неверный пароль (Negative Path)

1. **gRPC (Auth):** `Unauthenticated`.
2. **Ответ (BFF):** HTTP **401**, `{ "error": "…" }`.

---

## SR-AGG-AUTH-03: Refresh токена {#SR-AGG-AUTH-03}

Refresh позволяет продлить сессию **без повторного ввода пароля**, когда access token истёк, но refresh ещё действителен.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Без повторного login** | Клиент обновляет access token по refresh token. |
| **Публичный endpoint** | Refresh не требует валидного access JWT — только `RefreshTokenDto`. |
| **Rate limit** | Policy `auth-public`, как у login. |

### 2. Высокоуровневое описание

Представим refresh как **продление пропуска без повторного паспортного контроля**.

1. **Сотрудник (Frontend):** замечает, что пропуск (access token) просрочен — turnstile не пускает на `/api/Projects`.
2. **Обращение на стойку:** отправляется **длинный бланк продления** (refresh token) — не пароль.
3. **Aggregator:** передаёт бланк в authorization-module (`RefreshToken`).
4. **Auth:** если refresh валиден и не отозван — выдаёт **новую пару** tokens.
5. **Повтор запроса:** frontend прозрачно повторяет исходный API call с новым access token.

Таким образом, UX остаётся «бесшовным», а Aggregator снова выступает только транспортом — решение о валидности refresh принимает auth-сервис.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/Auth/refresh-token`.
* **Downstream:** gRPC `RefreshToken`.

#### Сценарий А: Тихое продление (Happy Path)

1. **401 на API:** access expired; axios/fetch interceptor перехватывает.
2. **Refresh:** POST `/api/Auth/refresh-token` с `{ refreshToken }`.
3. **Ответ:** HTTP **200**, новая пара tokens; interceptor сохраняет и **retry** исходного запроса.

#### Сценарий Б: Refresh отозван (Negative Path)

1. **gRPC (Auth):** `Unauthenticated` (logout или revoke).
2. **Ответ (BFF):** HTTP **401**; frontend редиректит на login.

---

## SR-AGG-AUTH-04: Подтверждение email {#SR-AGG-AUTH-04}

Подтверждение email завершает цикл регистрации: пользователь переходит по ссылке из письма, а Aggregator проксирует одноразовый token в auth-сервис.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **GET callback** | Query `userId` и `token` из ссылки письма. |
| **Публичный доступ** | JWT не требуется; rate limit `auth-public`. |
| **Маппинг** | `NotFound` → 404, `InvalidArgument` → 400. |

### 2. Высокоуровневое описание

Представим confirm email как **активацию ключа от почтового ящика**.

1. **Пользователь:** получает письмо со ссылкой; клик открывает GET в браузере.
2. **Aggregator:** не знает заранее, валидна ли ссылка — передаёт `userId` + `token` в auth (`ConfirmEmail`).
3. **authorization-module:** находит запись подтверждения, помечает email verified.
4. **UI:** показывает сообщение из `AuthResponseDto`.

Ссылка собирается на стороне auth/SMTP (`AUTH_CONFIRMATION_LINK` в deploy); BFF только принимает готовые query-параметры.

Таким образом, Aggregator не генерирует confirm tokens — он **доставляет** их в auth-сервис так же, как доставляет login credentials.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** GET `/api/Auth/confirm-email?userId=…&token=…`.
* **Downstream:** gRPC `ConfirmEmail`.

#### Сценарий А: Переход по ссылке (Happy Path)

1. **Браузер:** GET с query из email.
2. **gRPC (BFF → Auth):** `ConfirmEmail`.
3. **Ответ:** HTTP **200**, `AuthResponseDto`.

#### Сценарий Б: Просроченный или неверный token (Negative Path)

**Сценарий:** Пользователь переходит по старой ссылке из письма.

1. **GET** confirm-email с invalid `token` или unknown `userId`.
2. **gRPC (Auth):** `NotFound` или `InvalidArgument`.
3. **Маппинг (BFF):** HTTP **404** или **400**, `{ "error": "<detail>" }`.
4. **UI:** страница «Ссылка недействительна»; повторная регистрация не требуется, если email уже подтверждён.

---

## SR-AGG-AUTH-05: Профиль текущего пользователя {#SR-AGG-AUTH-05}

Endpoint `/me` — канонический способ узнать «кто я» после login: email, display name, avatar. JWT доказывает личность локально; актуальные поля профиля запрашиваются у auth-сервиса.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT обязателен** | `[Authorize]`; без Bearer — 401 на middleware. |
| **Identity из claims** | `MappingHelper.GetUserId` — claim `sub` / `NameIdentifier`. |
| **Downstream lookup** | gRPC `GetUserInfo` + metadata `user_id`. |

### 2. Высокоуровневое описание

Представим `/me` как **сверку пропуска с актуальной карточкой сотрудника**.

1. **Охранник (JWT middleware):** проверяет подпись access token — «пропуск не подделан».
2. **Регистратор (MappingHelper):** читает номер сотрудника (`user_id`) с пропуска.
3. **Запрос в HR (gRPC):** Aggregator вызывает `GetUserInfo` — «дай актуальные ФИО и email для этого id».
4. **Ответ клиенту:** `UserInfoDto` для header, settings, hydration SPA.

Aggregator **не** хранит кэш профиля — каждый `/me` идёт в auth-module (stateless BFF).

Таким образом, JWT отвечает на вопрос «кто обращается», а gRPC — «какие у него сейчас данные профиля».

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** GET `/api/Auth/me`.
* **Downstream:** gRPC `GetUserInfo`.

#### Сценарий А: Hydration при старте SPA (Happy Path)

1. **App load:** frontend имеет access token в storage.
2. **GET /me** с Bearer header.
3. **JWT validation → GetUserInfo →** HTTP **200**, `UserInfoDto`.

#### Сценарий Б: Просроченный access (Negative Path)

1. **JWT middleware:** token expired → HTTP **401**; gRPC **не вызывается**.

---

## SR-AGG-AUTH-06: Logout {#SR-AGG-AUTH-06}

Logout завершает сессию на стороне auth-сервиса (отзыв refresh) и на клиенте (удаление tokens).

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT + refresh** | Access JWT + body `LogoutDto.refreshToken`. |
| **Инвалидация на auth** | gRPC `LogoutUser`. |
| **Клиентская очистка** | Frontend удаляет tokens после 200. |

### 2. Высокоуровневое описание

Представим logout как **сдачу пропуска и аннулирование бланка продления**.

1. **Сотрудник:** нажимает «Выйти»; frontend отправляет **и** Bearer access, **и** refresh token в body.
2. **Aggregator:** из JWT знает userId; передаёт оба идентификатора в `LogoutUser`.
3. **authorization-module:** помечает refresh недействительным.
4. **Клиент:** очищает storage; дальнейший refresh/login — только заново.

Aggregator не ведёт серверный реестр «активных сессий» — stateless шлюз.

Таким образом, даже если access token ещё не истёк по `exp`, **refresh уже нельзя использовать** для продления после успешного logout.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/Auth/logout`.
* **Downstream:** gRPC `LogoutUser`.

#### Сценарий А: Выход (Happy Path)

1. POST logout с Bearer + `{ refreshToken }`.
2. **gRPC LogoutUser** → HTTP **200**, `AuthResponseDto`.
3. Frontend → redirect login, clear storage.

---

## SR-AGG-AUTH-07: Обновление username, password, avatar URL {#SR-AGG-AUTH-07}

Пользователь меняет отображаемое имя, пароль или URL аватара. Identity берётся **только из JWT**, не из body — защита от подмены userId.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT обязателен** | Три PUT-маршрута под `[Authorize]`. |
| **userId из claims** | Body не содержит target user id. |
| **Три RPC** | `UpdateUsername`, `UpdatePassword`, `UpdateAvatarUrl`. |

### 2. Высокоуровневое описание

Представим обновление профиля как **изменение данных в HR через стойку, где уже проверен пропуск**.

1. **Сотрудник (Frontend):** меняет имя, пароль или ссылку на фото в settings.
2. **Охранник (JWT):** подтверждает личность по access token.
3. **Aggregator:** подставляет `userId` из claims в gRPC request — клиент **не может** указать чужой id.
4. **authorization-module:** применяет изменение к своей БД пользователей.
5. **Avatar URL:** файл загружается отдельно через Media (`/api/Media/upload-image`); PUT avatar-url сохраняет только строку URL.

Таким образом, все мутации профиля проходят через **один доверенный канал identity** — JWT claims на BFF.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршруты:** PUT `/api/Auth/username`, `/password`, `/avatar-url`.
* **Downstream:** соответствующие gRPC методы AuthService.

#### Сценарий А: Смена display name (Happy Path)

1. PUT `/api/Auth/username` + Bearer, body `UpdateUsernameDto`.
2. BFF: `GetUserId` → `UpdateUsername` с metadata.
3. HTTP **200**, `AuthResponseDto`; UI обновляет header.

#### Сценарий Б: Смена пароля (Happy Path)

1. PUT `/api/Auth/password` с currentPassword и newPassword.
2. Auth проверяет текущий hash → обновляет.
3. HTTP **200**; frontend по политике может вызвать refresh или re-login.

---

## SR-AGG-AUTH-08: Rate limiting публичной аутентификации {#SR-AGG-AUTH-08}

Публичные auth endpoints — главная поверхность для brute-force и spam registration. Aggregator ограничивает частоту **до** вызова authorization-module.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Fixed window** | 10 запросов / минуту / IP. |
| **Partition key** | `X-Forwarded-For` (первый hop) или `RemoteIpAddress`. |
| **Scope** | register, login, refresh-token, confirm-email. |
| **In-memory** | Per-instance; см. [[02 - КАР-5 - Rate Limiting публичной аутентификации]]. |

### 2. Высокоуровневое описание

Представим rate limit как **очередь на стойке с ограничением «не более 10 человек в минуту»**.

1. **Посетитель (Client IP):** каждый POST login/register увеличивает счётчик окна.
2. **Счётчик (Rate limiter middleware):** живёт в памяти процесса Aggregator; окно 1 минута, лимит 10.
3. **11-й запрос:** turnstile закрывается — HTTP **429** **без** gRPC в auth.
4. **Защищённые routes** (`/me`, logout, profile PUT): **не** под `auth-public` — у них другая модель (JWT required).

Таким образом, auth-module защищён от простого flood даже при компрометации одного frontend origin.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Policy:** `auth-public`, `[EnableRateLimiting]` на actions `AuthController`.
* **Ответ при reject:** `{ "error": "Too many requests. Please try again later." }`.

#### Сценарий А: Brute-force login (Negative Path)

1. 11 POST `/api/Auth/login` с одного IP за 60 секунд.
2. 11-й → HTTP **429**; authorization-module не вызывается.

---

*Следующая группа: [[02 - Контент - проекты и колоды (Content)]].*
