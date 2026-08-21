# SMTP Email (Confirm)

## Введение

`EmailService` отправляет письмо подтверждения при регистрации через **FluentEmail + SmtpSender** (SSL).

**SR:** SR-AUTHMOD-REG-01

---

## Конфигурация

| Key | Описание |
| :--- | :--- |
| Email:Host | SMTP host |
| Email:Port | SMTP port (587 typical) |
| Email:UserName | SMTP credentials |
| Email:Password | SMTP password |
| Email:Address | From address |
| Email:DisplayName | From display name |
| ConfirmationLink | Base URL prefix; appended `={userId}&token={encoded}` |

Production: все ключи validated at startup (SR-AUTHMOD-OPS-03).

---

## Поведение

| Шаг | Действие |
| :--- | :--- |
| 1 | Subject: «Confirm your email» |
| 2 | Body: link with Identity confirmation token |
| 3 | Failure → register throws; gRPC InvalidArgument |

---

## Статус

| Исход | Результат |
| :--- | :--- |
| SMTP success | Register continues |
| SMTP failure | «Failed to send confirmation email» |
