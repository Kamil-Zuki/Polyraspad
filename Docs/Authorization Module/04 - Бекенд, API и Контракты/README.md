# Backend, API и контракты — Authorization Module



## Статус `04`



| Подпапка | Статус |

| :--- | :--- |

| `Методы API/gRPC/` | ✅ `authorization.proto`, 3 group files (10 RPC), эталонные блоки |

| `Методы API/DTO/` | ✅ `00` + `01` Auth/profile messages |

| `Методы API/REST API/` | ✅ Legacy `01` + BFF cross-links |

| `Интеграции/` | ✅ SMTP |

| `Алгоритмы/` | ✅ JWT, email, gRPC context, platform ops |



## Дерево



```

04 - Бекенд, API и Контракты/

├── Методы API/gRPC/

│   ├── 00 - gRPC - Общая информация.md

│   ├── authorization.proto

│   ├── 01 - Регистрация и подтверждение email (Registration).md

│   ├── 02 - Аутентификация и JWT-токены (Authentication).md

│   └── 03 - Управление профилем (Profile Management).md

├── Методы API/REST API/

│   ├── 00 - REST API - Общая информация.md

│   └── 01 - Аутентификация (Legacy REST).md

├── Методы API/DTO/

│   ├── 00 - DTO - Общая информация.md

│   └── 01 - Аутентификация и профиль (Auth).md

├── Интеграции со сторонними сервисами/

│   ├── 00 - Интеграции - Общая информация.md

│   └── 01 - SMTP Email (Confirm).md

└── Алгоритмы и методы бекенда/

    ├── 00 - Алгоритмы - Общая информация.md

    ├── 01 - JWT и Refresh Token Generation.md

    ├── 02 - Email Confirmation Flow.md

    ├── 03 - gRPC User Context Extraction.md

    └── 04 - Platform Operations.md

```



**Proto source:** `authorization.proto` в папке gRPC + `authorization-module.API/Protos/authorization.proto`



**Caller:** Aggregator Service (`IAuthorizationClient` / gRPC)



**Публичный REST:** [[Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/01 - Аутентификация и профиль (Auth)]]


