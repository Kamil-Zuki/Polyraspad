# JWT и Refresh Token Generation

## Введение

`TokenService` — symmetric JWT и cryptographically secure refresh tokens.

**SR:** SR-AUTHMOD-AUTH-04, SR-AUTHMOD-AUTH-02

---

## JWT Access — вход/выход

| Вход | Выход |
| :--- | :--- |
| userId, userName | JWT string |

## Алгоритм JWT

1. Claims: `sub`=userId, `name`=userName, `jti`=Guid.NewGuid().
2. Signing: HMAC-SHA256, key = UTF8(`Jwt:Secret`).
3. Issuer/Audience from config.
4. Expires: UtcNow + `Jwt:Expire` minutes.

---

## Refresh token

1. `RandomNumberGenerator` 64 bytes → Base64 string.
2. Persist RefreshToken entity, ExpiryDate = UtcNow + 7 days.
3. On refresh: set old IsRevoked=true, insert new row.

---

## Псевдокод rotation

```
stored = DB.Find(refresh_token)
if stored is null or revoked or expired → error
user = Users.Find(stored.UserId)
stored.IsRevoked = true
newRefresh = GenerateRefreshToken()
DB.Add(newRefresh for user)
return JWT(user) + newRefresh
```
