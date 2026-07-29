# План реализации повторной отправки письма подтверждения (Resend Confirmation Email)

**Цель:** Добавить возможность пользователю повторно запросить письмо подтверждения аккаунта (`EmailConfirmed`), если первоначальное письмо потерялось, устарело или не пришло.  
**Продуктовый контекст:** [01 - Регистрация и подтверждение email (Registration).md](../Authorization%20Module/01%20-%20Функциональная%20спецификация/Возможности%20сервиса/01%20-%20Регистрация%20и%20подтверждение%20email%20(Registration).md).

---

## 1. Архитектура и взаимодействие

```
[ Frontend (/auth) ]
        │  POST /api/Auth/resend-confirmation (JSON: { "email": "..." })
        ▼
[ AggregatorService ]
        │  gRPC AuthService.ResendConfirmationEmail(ResendConfirmationEmailRequest)
        ▼
[ authorization-module ]
        │  UserManager.FindByEmailAsync(email)
        │  UserManager.GenerateEmailConfirmationTokenAsync(user)
        │  EmailService.SendEmailAsync(...)
        ▼
   SMTP Server
```

---

## 2. Пошаговый план реализации (Checklist)

### Шаг 1: `authorization-module` (Identity & gRPC)

- [ ] **1.1. gRPC контракт (`authorization.proto`):**
  - Добавить сообщение `ResendConfirmationEmailRequest` с полем `string email = 1;`.
  - Добавить RPC-метод `rpc ResendConfirmationEmail (ResendConfirmationEmailRequest) returns (MessageResponse);` в сервис `AuthService`.
- [ ] **1.2. DTO и интерфейс:**
  - Создать DTO `ResendConfirmationEmailRequestDto.cs` в `Dtos/`:
    ```csharp
    public record ResendConfirmationEmailRequestDto(string Email);
    ```
  - Добавить метод в `IAuthService.cs`:
    ```csharp
    Task<StringResultDto> ResendConfirmationEmailAsync(string email);
    ```
- [ ] **1.3. Логика сервиса (`AuthService.cs`):**
  - Реализовать `ResendConfirmationEmailAsync(string email)`:
    - Проверить наличие email.
    - Найти пользователя (`_userManager.FindByEmailAsync`).
    - Если пользователь не найден — выбросить `ResponseException("User not found")`.
    - Если `user.EmailConfirmed == true` — выбросить `ResponseException("Email is already confirmed")`.
    - Сгенерировать новый токен (`GenerateEmailConfirmationTokenAsync`) и отправить письмо через `_emailService.SendEmailAsync`.
    - Вернуть `new StringResultDto("Confirmation email sent")`.
- [ ] **1.4. gRPC-контроллер (`Api/Grpc/AuthService.cs`):**
  - Переопределить `ResendConfirmationEmail(ResendConfirmationEmailRequest request, ServerCallContext context)` и пробросить вызов в `_authService.ResendConfirmationEmailAsync(request.Email)`.

---

### Шаг 2: `AggregatorService` (REST API / BFF)

- [ ] **2.1. Синхронизация proto:**
  - Обновить `AggregatorService/Protos/authorization.proto` (добавить `ResendConfirmationEmailRequest` и метод `ResendConfirmationEmail`).
- [ ] **2.2. DTO:**
  - Создать `AggregatorService/Dtos/Auth/ResendConfirmationEmailDto.cs`:
    ```csharp
    public record ResendConfirmationEmailDto(string Email);
    ```
- [ ] **2.3. Клиент авторизации (`IAuthorizationServiceClient` / `AuthorizationServiceClientImpl`):**
  - Добавить метод:
    ```csharp
    Task<AuthResponseDto> ResendConfirmationEmailAsync(ResendConfirmationEmailDto request, CancellationToken cancellationToken = default);
    ```
  - Реализовать вызов gRPC клиента `.ResendConfirmationEmailAsync(...)`.
- [ ] **2.4. REST-контроллер (`AuthController.cs`):**
  - Добавить эндпоинт `POST /api/Auth/resend-confirmation`:
    - Проверить rate limiting (`auth-public`).
    - Вызвать клиент и вернуть `200 OK` с сообщением при успехе или соответствующую HTTP-ошибку при `RpcException`.

---

### Шаг 3: `polyraspad-frontend` (UI & API Client)

- [ ] **3.1. API клиент (`auth-client.ts` и `constants.ts`):**
  - В `constants.ts` добавить эндпоинт:
    ```ts
    RESEND_CONFIRMATION: "/api/Auth/resend-confirmation",
    ```
  - В `auth-client.ts` добавить метод:
    ```ts
    async resendConfirmationEmail(email: string): Promise<AuthResponseDto> {
      return this.request<AuthResponseDto>(API_ENDPOINTS.AUTH.RESEND_CONFIRMATION, {
        method: "POST",
        body: JSON.stringify({ email }),
      });
    }
    ```
- [ ] **3.2. UI (`src/app/auth/page.tsx`):**
  - При возникновении ошибки входа с текстом `"Email not confirmed"` отображать кнопку / ссылку:
    **«Отправить письмо подтверждения повторно»**.
  - По нажатию вызывать `apiClient.auth.resendConfirmationEmail(email)`, показывать индикатор загрузки и уведомление об успешной отправке письма.

---

### Шаг 4: Тестирование и проверка

- [ ] **4.1. Сборка бэкенда и фронтенда:**
  - Проверить компиляцию всех трёх сервисов (`authorization-module`, `AggregatorService`, `polyraspad-frontend`).
- [ ] **4.2. Ручная проверка E2E:**
  - Запустить контейнеры через `docker compose up --build -d authorization-module aggregator-service polyraspad-frontend`.
  - Попробовать залогиниться под неподтверждённым аккаунтом.
  - Нажать кнопку повторной отправки письма в UI и проверить в логах/почте получение новой ссылки подтверждения.
