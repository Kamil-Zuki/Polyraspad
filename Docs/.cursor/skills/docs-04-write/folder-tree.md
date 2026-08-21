# Folder 04 tree (etalon)

Mirror structure from `(Done) Authorization Service/04 - Бекенд, API и Контракты/`. File **names** follow target service **`01`** groups — counts may differ from Auth.

```
04 - Бекенд, API и Контракты/
├── Методы API/
│   ├── DTO/
│   │   ├── 00 - DTO - Общая информация.md
│   │   └── NN - {Group from 01}.md
│   ├── REST API/
│   │   ├── 00 - REST API - Общая информация.md
│   │   └── NN - {Group}.md
│   ├── Socket/
│   │   ├── 00 - WebSocket API - Общая информация.md
│   │   └── NN - {Group}.md
│   └── gRPC/
│       ├── 00 - gRPC - Общая информация.md
│       ├── NN - {Group}.md
│       └── {service}_service.proto
├── Интеграции со сторонними сервисами/
│   ├── 00 - Интеграции … - Общая информация.md
│   └── NN - {Integration}.md
├── Работа с Rabbit MQ/
│   ├── 00 - Работа с Rabbit MQ - Общая информация.md
│   └── NN - {Group}.md
├── Работа с Redis/
│   ├── 00 - Работа с Redis - Общая информация.md
│   └── NN - {Group}.md
└── Алгоритмы и методы бекенда/
    ├── 00 - Алгоритмы … - Общая информация.md
    └── NN - {Group}.md
```

**Optional:** `gRPC/02 - Спецификация proto (….proto).md` — fenced proto wrapper when split from main group files.

Compare target service tree to Auth when building manifest — list missing/extra files explicitly.
