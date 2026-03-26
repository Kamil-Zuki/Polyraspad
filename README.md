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

Для работы AI в редакторе (генерация примеров, перевод, грамматика) нужен запущенный [Ollama](https://ollama.com) и модель. По умолчанию в проекте задана **qwen2.5:1.5b** (меньше по размеру, чем qwen3.5:2b; без облачного API-ключа).

**Локальный запуск (без Docker):**

1. Установите Ollama и запустите сервер (если не запущен как служба):
   ```bash
   ollama serve
   ```
2. Скачайте модель:
   ```bash
   ollama pull qwen2.5:1.5b
   ```
3. Для фронта при локальной разработке задайте (по желанию):
   - `OLLAMA_BASE_URL=http://localhost:11434` (по умолчанию так и есть)
   - `OLLAMA_MODEL=qwen2.5:1.5b` — модель по умолчанию для generate

**Проверка:** список моделей — `GET http://localhost:11434/api/tags` или через приложение: запрос к `/api/ollama/models`. Генерация — `POST /api/ollama/generate` с телом `{ "prompt": "Hello", "model": "qwen2.5:1.5b" }`.

В Docker образ Ollama уже входит (`docker compose up`), по умолчанию используется модель **qwen2.5:1.5b** (переменные `OLLAMA_MODEL` и `Ollama__Model`).
