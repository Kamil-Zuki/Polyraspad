# 04 — Бекенд, API и Контракты



| Подпапка | Назначение | Статус |

| :--- | :--- | :--- |

| [[Методы API/gRPC/00 - gRPC - Общая информация]] | Service `media.MediaService`, metadata `user_id`, inventory 15 RPC | Done |

| [[Методы API/gRPC/01 - Загрузка и выдача медиа (Media Storage)]] | Upload + Get*Url (6 RPC) | Done |

| [[Методы API/gRPC/02 - Reader Library — книги (Reader Books)]] | List/Save/Delete books (3 RPC) | Done |

| [[Методы API/gRPC/03 - Reader Library — коллекции и шаринг (Reader Collections)]] | Collections CRUD + share (6 RPC) | Done |

| [[Методы API/gRPC/04 - Платформенные контракты (Operations)]] | HTTP `/healthz`, `user_id` rules (no proto RPC) | Done |

| [[Методы API/gRPC/media.proto]] | Proto copy from `MediaService/Protos/media.proto` | Done |

| [[Интеграции со сторонними сервисами/00 - Интеграции - Общая информация]] | MinIO (outbound), Aggregator (inbound caller) | Done |

| [[Интеграции со сторонними сервисами/01 - MinIO (S3)]] | S3 API via AWS SDK | Done |

| [[Интеграции со сторонними сервисами/02 - Aggregator Service (gRPC caller)]] | JWT → `user_id` metadata | Done |

| [[Алгоритмы и методы бекенда/00 - Алгоритмы и методы бекенда - Общая информация]] | Inventory алгоритмов | Done |

| [[Алгоритмы и методы бекенда/01 - Загрузка и выдача медиа (Media Storage)]] | S3 keys, upload, dual URL model | Done |

| [[Алгоритмы и методы бекенда/02 - Reader Library — книги (Reader Books)]] | JSON library index, URL hydration | Done |

| [[Алгоритмы и методы бекенда/03 - Reader Library — коллекции и шаринг (Reader Collections)]] | Collections index, share scan | Done |

| [[Алгоритмы и методы бекенда/04 - Платформенные контракты (Operations)]] | `user_id` extraction, health | Done |



**Не используется в Media Service:** REST API (публичный REST на Aggregator), WebSocket, DTO layer (proto messages = contract), Redis, RabbitMQ.



Proto source of truth: `MediaService/Protos/media.proto` (копия в [[Методы API/gRPC/media.proto]]).



Public REST mapping: `Docs/Aggregator Service/04 - Бекенд, API и Контракты/Методы API/REST API/09 - Медиа и Reader Library (Media).md`.



Liveness: `GET /healthz` на порту **5121** (HTTP/2 + gRPC h2c) — см. [[01 - Функциональная спецификация/Возможности сервиса/04 - Платформенные контракты (Operations)#SR-MEDIA-OPS-01]].



## Дерево папки



```

04 - Бекенд, API и Контракты/

├── README.md

├── Методы API/

│   └── gRPC/

│       ├── media.proto

│       ├── 00 - gRPC - Общая информация.md

│       ├── 01 - Загрузка и выдача медиа (Media Storage).md

│       ├── 02 - Reader Library — книги (Reader Books).md

│       ├── 03 - Reader Library — коллекции и шаринг (Reader Collections).md

│       └── 04 - Платформенные контракты (Operations).md

├── Интеграции со сторонними сервисами/

│   ├── 00 - Интеграции - Общая информация.md

│   ├── 01 - MinIO (S3).md

│   └── 02 - Aggregator Service (gRPC caller).md

└── Алгоритмы и методы бекенда/

    ├── 00 - Алгоритмы и методы бекенда - Общая информация.md

    ├── 01 - Загрузка и выдача медиа (Media Storage).md

    ├── 02 - Reader Library — книги (Reader Books).md

    ├── 03 - Reader Library — коллекции и шаринг (Reader Collections).md

    └── 04 - Платформенные контракты (Operations).md

```


