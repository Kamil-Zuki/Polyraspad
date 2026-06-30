# Введение

Группа алгоритмов инфраструктуры OIDC и Интеграции — это техническая прослойка, обеспечивающая криптографически безопасный диалог между локальной платформой (Authorization Service) и глобальным провайдером идентичности (STEOS ID). 

Эти алгоритмы гарантируют, что в локальную систему попадут только те пользователи, чья личность математически подтверждена доверенным IdP. Здесь реализована оркестрация редиректов по стандарту OIDC Authorization Code Flow, асинхронная инвалидация через брокер сообщений для защиты от задержек отзыва прав (Revocation Lag) и механизмы агрессивного кэширования ключей проверки подписей (JWKS), чтобы исключить сетевые задержки и снизить зависимость от постоянной доступности глобального провайдера.

# 1. Список алгоритмов OIDC и Интеграции

В данном разделе представлены алгоритмы, обеспечивающие интеграцию и валидацию доверия с корневым Identity Provider.

| **Название алгоритма** | **Краткое описание** |
| :--- | :--- |
| **Алгоритм обработки OIDC Authorization Code Flow** | Оркестрирует редиректы, генерирует и проверяет параметры `state` и `nonce` (защита от CSRF/Replay), а также выполняет S2S обмен кода на токены. |
| **Алгоритм фонового кэширования и In-Memory валидации JWKS** | Асинхронно скачивает публичные RSA-ключи от STEOS ID и валидирует подписи входящих JWT-токенов в RAM за доли миллисекунды. |
| **Алгоритм асинхронной инвалидации (Back-Channel Logout)** | Прослушивает очередь RabbitMQ и мгновенно уничтожает локальные сессии (Redis) при получении сигнала о глобальной блокировке пользователя. |

---

# Алгоритм обработки OIDC Authorization Code Flow

## Контекст и область применения

Данный раздел описывает алгоритм защиты процесса авторизации при перенаправлении пользователя между Платформой и STEOS ID.

### Почему был создан

Стандартный поток авторизации подразумевает уход пользователя на другой домен (STEOS ID) и его возврат с кодом (Authorization Code). Если не защитить этот процесс криптографическими метками (`state` и `nonce`), система становится уязвимой к атакам CSRF (когда злоумышленник "подсовывает" пользователю свой код авторизации) и Replay-атакам (повторное использование одного и того же кода).

### Бизнес-требование

Система должна реализовывать защищенный OIDC поток авторизации со строгой валидацией параметров `state` и `nonce` при редиректах на STEOS ID и обратно (SR-AUTH-OI-01).

### Область применения

| **№** | **Описание** |
| :--- | :--- |
| **1** | Пользователь нажимает кнопку "Войти через STEOS ID". |
| **2** | Обработка Callback-запроса (возврат с глобального портала после ввода пароля). |

### Ограничения применения

| **№** | **Описание** |
| :--- | :--- |
| **1** | Алгоритм требует возможности установки временных (Temp) Cookie в браузере для хранения `state`. |
| **2** | Для обмена кода на токен требуется доступность эндпоинта `/token` на стороне STEOS ID (S2S-вызов). |

## Входные данные

| **Параметр** | **Тип данных** | **Описание** | **Ограничения** | **Обязательность** |
| :--- | :--- | :--- | :--- | :--- |
| `AuthorizationCode` | `string` | Код, пришедший от STEOS ID в параметре URL | Живет обычно 1 минуту | Да |
| `State` | `string` | Возвращенный параметр state | Должен совпадать с исходным | Да |

## Выходные данные

| **Параметр** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `Tokens` | `OidcTokenResponse` | Объект, содержащий Access, ID и Refresh токены |
| `RedirectUrl` | `string` | URL для финального редиректа пользователя в SPA |

## Логика работы (Псевдокод)

Алгоритм состоит из двух фаз: Инициализация (генерация state и редирект) и Завершение (валидация, обмен кода на токены).

```csharp
// ФАЗА 1: ИНИЦИАЛИЗАЦИЯ (GET /api/auth/login)
public HttpResponseMessage InitiateLogin(string redirectUrl)
{
    // Генерируем криптографически стойкие случайные строки
    string state = GenerateCryptoRandomString();
    string nonce = GenerateCryptoRandomString();
    
    // Внедрение поддержки PKCE (Proof Key for Code Exchange)
    string codeVerifier = GenerateCryptoRandomString();
    string codeChallenge = GenerateSha256Hash(codeVerifier);

    // Сохраняем эти параметры во временной, зашифрованной Cookie
    // Cookie живет, например, 10 минут (на время ввода пароля пользователем)
    var tempCookieData = new { State = state, Nonce = nonce, CodeVerifier = codeVerifier, RedirectUrl = redirectUrl };
    SetEncryptedTempCookie("steos_auth_flow", tempCookieData);

    // Формируем URL для редиректа на STEOS ID
    string oidcUrl = $"https://id.steos.io/authorize?client_id={_clientId}&response_type=code&scope=openid profile email&redirect_uri={_callbackUrl}&state={state}&nonce={nonce}&code_challenge={codeChallenge}&code_challenge_method=S256";

    return Redirect(oidcUrl);
}

// ФАЗА 2: ЗАВЕРШЕНИЕ (GET /api/auth/callback)
public async Task<TokensResultDto> HandleCallbackAsync(string code, string state)
{
    // 1. Читаем и расшифровываем временную Cookie
    var flowData = GetAndRemoveEncryptedTempCookie("steos_auth_flow");
    if (flowData == null) throw new DomainException("OIDC_FLOW_TIMEOUT_OR_CSRF");

    // 2. Строгая проверка State (защита от CSRF)
    if (!ConstantTimeEquals(flowData.State, state))
    {
        throw new SecurityException("CSRF_STATE_MISMATCH");
    }

    // 3. Server-to-Server вызов: Обмен кода на токены (используя PKCE verifier)
    var tokenRequest = new Dictionary<string, string>
    {
        {"grant_type", "authorization_code"},
        {"client_id", _clientId},
        {"client_secret", _clientSecret}, // Или TLS сертификат
        {"code", code},
        {"redirect_uri", _callbackUrl},
        {"code_verifier", flowData.CodeVerifier}
    };
    
    var tokenResponse = await _oidcClient.PostFormAsync("https://id.steos.io/token", tokenRequest);
    
    if (tokenResponse.IsError) throw new DomainException("TOKEN_EXCHANGE_FAILED");

    // 4. Валидация ID Token (содержит Nonce)
    var idToken = ParseAndValidateJwt(tokenResponse.IdToken); // Проверка подписи см. КАР-8
    
    if (!ConstantTimeEquals(idToken.Claims["nonce"], flowData.Nonce))
    {
        throw new SecurityException("REPLAY_ATTACK_NONCE_MISMATCH");
    }

    return new TokensResultDto { Tokens = tokenResponse, FinalRedirectUrl = flowData.RedirectUrl };
}
```

---

# Алгоритм фонового кэширования и In-Memory валидации JWKS

## Контекст и область применения

### Почему был создан

Каждый раз при логине или тихом продлении (Silent Refresh), сервис получает JWT от STEOS ID. Проверять его подпись синхронным HTTP-вызовом за ключом к глобальному IdP — это долго (200-300 мс) и нестабильно (точка отказа). Алгоритм решает это, загружая публичные ключи в RAM и производя валидацию силами процессора (CPU) за доли миллисекунд.

### Бизнес-требование

Система должна кэшировать ключи (JWKS) и валидировать математические подписи JWT в памяти, без сетевых запросов при каждой авторизации (SR-AUTH-OI-02).

### Область применения

| **№** | **Описание** |
| :--- | :--- |
| **1** | Валидация `id_token` и `access_token` после OIDC Callback. |
| **2** | Фоновое периодическое скачивание ключей от STEOS ID (Background Worker). |

## Логика работы (Псевдокод)

Алгоритм состоит из фонового воркера (скачивание) и быстрого метода валидации.

```csharp
// ЧАСТЬ 1: ФОНОВОЕ СКАЧИВАНИЕ (JWKS Refresher - запускается раз в час)
public async Task RefreshJwksCacheAsync()
{
    // Скачиваем публичные ключи (JSON Web Key Set)
    var jwksResponse = await _httpClient.GetAsync("https://id.steos.io/.well-known/jwks.json");
    string jwksJson = await jwksResponse.Content.ReadAsStringAsync();
    
    var keySet = new JsonWebKeySet(jwksJson);
    var securityKeys = new List<SecurityKey>();

    foreach (var key in keySet.Keys)
    {
        // Конвертируем JSON-представление RSA-ключа в объект C# SecurityKey
        var rsaParameters = new RSAParameters
        {
            Modulus = Base64UrlEncoder.DecodeBytes(key.N),
            Exponent = Base64UrlEncoder.DecodeBytes(key.E)
        };
        var rsaKey = new RsaSecurityKey(rsaParameters) { KeyId = key.Kid };
        securityKeys.Add(rsaKey);
    }

    // Сохраняем в потокобезопасный In-Memory Cache (Singleton)
    _inMemoryKeyCache.Set("steos_jwks", securityKeys, TimeSpan.FromHours(24));
}

// ЧАСТЬ 2: IN-MEMORY ВАЛИДАЦИЯ (Синхронно)
public ClaimsPrincipal ValidateToken(string jwtTokenString)
{
    // 1. Читаем заголовок токена (без верификации подписи), чтобы узнать Key ID (kid)
    var handler = new JwtSecurityTokenHandler();
    var unvalidatedToken = handler.ReadJwtToken(jwtTokenString);
    var kid = unvalidatedToken.Header.Kid;

    // 2. Ищем ключ в In-Memory Cache
    var cachedKeys = _inMemoryKeyCache.Get<List<SecurityKey>>("steos_jwks");
    var signingKey = cachedKeys?.FirstOrDefault(k => k.KeyId == kid);

    if (signingKey == null)
    {
        // ФОЛЛБЭК: Если STEOS ID внезапно ротировал ключи, а кэш еще не обновился
        // Делаем экстренное форсированное обновление
        RefreshJwksCacheAsync().GetAwaiter().GetResult(); 
        cachedKeys = _inMemoryKeyCache.Get<List<SecurityKey>>("steos_jwks");
        signingKey = cachedKeys?.FirstOrDefault(k => k.KeyId == kid);
        
        if (signingKey == null) throw new SecurityException("UNKNOWN_SIGNING_KEY");
    }

    // 3. Выполняем чисто математическую (CPU) проверку подписи RSA-256
    var validationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = "https://id.steos.io",
        ValidateAudience = true,
        ValidAudience = _clientId,
        ValidateLifetime = true // Проверяем поле exp
    };

    var principal = handler.ValidateToken(jwtTokenString, validationParameters, out var validatedToken);
    return principal; // Токен 100% доверенный
}
```

---

# Алгоритм асинхронной инвалидации (Back-Channel Logout)

## Контекст и область применения

### Почему был создан

В распределенных системах возникает "задержка инвалидации". Если администратор блокирует пользователя (уволенного сотрудника) в глобальном STEOS ID, его локальная сессия в Authorization Service живет до истечения таймера (до 30 минут). В это "окно уязвимости" можно успеть скачать данные компании. Back-Channel Logout решает эту проблему.

### Бизнес-требование

Система должна слушать брокер сообщений (RabbitMQ). При глобальном бане локальный микросервис за миллисекунды убивает все локальные сессии юзера в Redis (SR-AUTH-OI-04).

### Область применения

| **№** | **Описание** |
| :--- | :--- |
| **1** | Глобальная блокировка пользователя администратором. |
| **2** | Смена пароля пользователем (принудительный Logout со всех устройств). |

## Логика работы (Псевдокод)

Алгоритм работает как бесконечный фоновый процесс (Consumer), ожидающий сообщений из очереди RabbitMQ.

```csharp
public async Task ProcessBackChannelLogoutEventAsync(string messageJson)
{
    // 1. Десериализуем сообщение из шины
    var logoutEvent = JsonSerializer.Deserialize<UserLogoutEventDto>(messageJson);
    Guid targetGlobalSteosId = logoutEvent.GlobalSteosId;

    // 2. Ищем все активные локальные сессии для этого пользователя в PostgreSQL
    // (Поскольку Redis не поддерживает SQL-поиск по значению, мы ищем UUID в БД)
    var activeSessions = await _sessionRepo.GetQueryable()
        .Where(s => s.GlobalSteosId == targetGlobalSteosId && s.IsActive)
        .ToListAsync();

    if (!activeSessions.Any()) return; // Сессий на этой инсталляции нет

    // 3. Собираем массив ключей для массового удаления в Redis
    var redisKeysToDelete = activeSessions
        .Select(s => (RedisKey)$"steos:sess:{s.SessionId}")
        .ToArray();

    // 4. Атомарное массовое удаление из Redis (O(N), где N обычно 1-5 сессий)
    // МГНОВЕННАЯ ИНВАЛИДАЦИЯ - с этой миллисекунды все API Gateway вызовы 
    // с этими Cookie вернут 401 Unauthorized
    await _redisDatabase.KeyDeleteAsync(redisKeysToDelete);

    // 5. Обновление стейта в PostgreSQL (Soft Delete)
    foreach (var session in activeSessions)
    {
        session.IsActive = false;
        session.RevokedAt = DateTime.UtcNow;
        session.RevokeReason = "BACK_CHANNEL_LOGOUT";
    }
    await _sessionRepo.UpdateRangeAsync(activeSessions);

    // 6. Журналирование инцидента безопасности (WORM)
    await _auditLogger.LogSecurityAlertAsync(targetGlobalSteosId, "FORCED_LOGOUT_PROCESSED");
}
```