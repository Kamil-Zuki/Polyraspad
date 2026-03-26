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
| Ollama (AI)       | http://localhost:11434 (если запущен отдельно) |

### Ollama и AI-фичи

Для работы AI в редакторе (генерация примеров, перевод, грамматика) нужен запущенный [Ollama](https://ollama.com) и модель. Имя модели задаётся через переменную `OLLAMA_MODEL` (фронт) и `Ollama:Model` (AggregatorService).

**Локальный запуск (без Docker):**

1. Установите Ollama и запустите сервер (если не запущен как служба):
   ```bash
   ollama serve
   ```
2. Скачайте нужную модель:
   ```bash
   ollama pull <model-name>
   ```
3. Для фронта при локальной разработке задайте (по желанию):
   - `OLLAMA_BASE_URL=http://localhost:11434` (по умолчанию так и есть)
   - `OLLAMA_MODEL=<model-name>` — модель для generate

**Проверка:** список моделей — `GET http://localhost:11434/api/tags` или через приложение: запрос к `/api/ollama/models`. Генерация — `POST /api/ollama/generate` с телом `{ "prompt": "Hello", "model": "<model-name>" }`.

В Docker образ Ollama уже входит (`docker compose up`), а модель задаётся переменными `OLLAMA_MODEL` и `Ollama__Model`.
