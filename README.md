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

### AI Assistant (редактор и reader)

Фичи AI идут через **внешний OpenAI-compatible API** (ключ на стороне Aggregator) и BFF-маршруты Next.js `POST/GET /api/ai/*`. Общий секрет BFF → Aggregator: заголовок `X-Ai-Proxy-Key` (переменные `AI_PROXY_API_KEY` в Next и `Ai__ProxyApiKey` в Aggregator).

**Минимальная конфигурация (Docker / локально):**

1. Задайте `OPENAI_API_KEY` (или другой провайдер с совместимым endpoint — тогда `AI_COMPLETION_BASE_URL`).
2. Задайте одинаковый `AI_PROXY_API_KEY` для `aggregator-service` и `polyraspad-frontend` в `.env`.
3. Опционально: `AI_COMPLETION_MODEL` (по умолчанию `gpt-4o-mini`).

**Альтернатива:** режим Gemini на стороне Next — `GEMINI_API_KEY` и `EDITOR_AI_PROVIDER=gemini` (см. `polyraspad-frontend/.env.example`).

Локальный Ollama в compose **не используется**.
