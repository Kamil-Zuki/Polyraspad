# ISSUE-002: RefreshToken.UserId без EF/DB FK и без unique на Token

## Тип

Пробел

## В двух словах

В `03` связь ApplicationUser → RefreshToken описана логически. Миграция создаёт `RefreshTokens` без FK на `AspNetUsers` и без unique index на `Token`. Документировано в entity как intentional gap / hardening; целостность только на уровне приложения.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 03 | Entity `RefreshToken.UserId`, indexes | Нет DB FK; нет unique на `Token` |
| код | Migration `AddApplicationUserAvatarUrl` CreateTable RefreshTokens | Только PK на `Id` |
| 01 | SR-AUTHMOD-AUTH-02 | Предполагает надёжный lookup по Token |

Путь (вторично): `authorization-module.API/Migrations/20260418204451_AddApplicationUserAvatarUrl.cs`

## Доказательство

`CreateTable("RefreshTokens", …)` — `PrimaryKey` only; нет `ForeignKey` / `CreateIndex` на `Token` или `UserId`.

## Рекомендуемое действие

Hardening migration: FK `UserId` → `AspNetUsers.Id` (+ cascade/restrict policy) и unique index на `Token`.

## Статус

Open
