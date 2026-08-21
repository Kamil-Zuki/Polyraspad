# Email Confirmation Flow

## Введение

Identity-generated one-time token + SMTP link. **SR:** SR-AUTHMOD-REG-01, SR-AUTHMOD-REG-02

---

## Register flow

1. `GenerateEmailConfirmationTokenAsync(user)`.
2. `Uri.EscapeDataString(token)`.
3. URL: `{ConfirmationLink}={user.Id}&token={encoded}`.
4. SendEmailAsync.

---

## Confirm flow

1. `FindByIdAsync(userId)`.
2. `ConfirmEmailAsync(user, token)` — URL-decoded token from client.
3. Delete unconfirmed users with same email.
4. Return success message.

---

## Cleanup rationale

Предотвращает накопление duplicate pending registrations на один email после успешного confirm.
