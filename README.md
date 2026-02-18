# Polyraspad project

## Запуск в Docker одной командой

Из корня репозитория (папка `Polyraspad/`):

```bash
docker compose up --build
```

Остановка и удаление контейнеров и volumes:

```bash
docker compose down -v
```

### Переменные окружения

Скопируйте [`.env.example`](.env.example) в `.env` и при необходимости измените значения (пароль Postgres, URL для фронта):

```bash
cp .env.example .env
```

### URL после запуска

| Сервис            | URL                      |
|-------------------|--------------------------|
| Фронтенд (Next.js)| http://localhost:3000    |
| Aggregator API    | http://localhost:5000    |
| Authorization     | http://localhost:5027    |
| Vocabulary (gRPC) | localhost:5117           |
| Inclusive (gRPC)  | localhost:40051          |
| Postgres          | localhost:5454 (user/pass см. в `.env`) |
