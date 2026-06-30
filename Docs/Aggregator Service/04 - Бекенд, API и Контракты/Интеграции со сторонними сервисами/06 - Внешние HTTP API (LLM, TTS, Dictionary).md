# Введение

Внешние HTTP API, вызываемые Aggregator напрямую (не gRPC).

# 1. OpenAI-compatible LLM

| Параметр | Значение |
| :--- | :--- |
| **Config** | `Ai:BaseUrl`, `Ai:ApiKey`, `AI_COMPLETION_*` env |
| **SR** | SR-AGG-AI-01 |
| **Auth** | Bearer provider API key (server-side only) |

Endpoints proxied: chat completions, mining-draft structured output.

# 2. TTS providers

| Provider | Config | SR |
| :--- | :--- | :--- |
| espeak-ng | `AI_TTS_PROVIDER=espeak` | SR-AGG-MEDIA-01 |
| Mistral TTS | `AI_TTS_VOICE_ID`, API key | SR-AGG-MEDIA-01 |

# 3. Translation — MyMemory

| Параметр | Значение |
| :--- | :--- |
| **Route BFF** | POST /api/integrations/translate |
| **SR** | SR-AGG-INT-02 |
| **Fallback** | original text + degraded flag |

# 4. Free Dictionary API

| Параметр | Значение |
| :--- | :--- |
| **Route BFF** | GET /api/integrations/dictionary |
| **SR** | SR-AGG-INT-03 |

# Resilience

Typed HttpClient + Polly: retry transient errors, circuit breaker 5 failures / 30s.

# Security

Provider keys только в env/K8s secrets. `X-Ai-Proxy-Key` — отдельный shared secret для Next.js BFF, не provider key.
