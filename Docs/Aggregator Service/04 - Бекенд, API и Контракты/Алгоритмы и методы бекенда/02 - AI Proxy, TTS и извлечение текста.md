# Введение

BFF-side обработка AI, TTS и document text extraction до/после вызова MediaService или external HTTP.

# 1. AI Proxy key validation + LLM forward (SR-AGG-AI-01)

## Контекст

Next.js BFF вызывает Aggregator с заголовком `X-Ai-Proxy-Key` (= `AI_PROXY_API_KEY` / `Ai:ProxyApiKey`). Aggregator не принимает provider API key от клиента.

## Логика

1. Compare constant-time proxy key OR validate JWT policy
2. Optional rate limit per userId / IP
3. Forward JSON body к `Ai:BaseUrl` completions endpoint
4. Stream или buffer response → `AiGenerateResponseDto`
5. Log latency; не логировать full prompt в production

## Ошибки

Provider 401/429 → 502/429 на REST. `AI_COMPLETION_ENABLED=false` → **503**.

---

# 2. TTS generation (SR-AGG-MEDIA-01)

## Контекст

`POST /api/Media/generate-audio` — espeak-ng локально в Docker или Mistral TTS HTTP.

## Ветвление

| `AI_TTS_PROVIDER` | Поведение |
| :--- | :--- |
| espeak | Spawn espeak-ng, return audio bytes / URL |
| mistral | HTTP TTS API с `AI_TTS_VOICE_ID` |

Invalid voice placeholder → **400** с явным message.

---

# 3. Document text extraction (SR-AGG-MEDIA-02)

## Контекст

PDF/EPUB/TXT upload → plain text для Reader import.

## Логика

1. Validate mime/size limits на BFF
2. Parse: iText/pdfium или delegate gRPC `ExtractDocumentText` на MediaService
3. Normalize whitespace, detect encoding (TXT)
4. Return `ExtractDocumentTextResponseDto`

## Ограничения

Max file size из config. Scanned PDF без OCR → partial empty text + warning flag.

---

# 4. Media HTTP proxy (SR-AGG-MEDIA-03)

## Контекст

`serve-image`, `serve-audio` — redirect или stream от MinIO public URL без exposing internal endpoint.

## Логика

Resolve storage key → `MINIO_PUBLIC_BASE_URL` + path → 302 или proxied stream с cache headers.

{#SR-AGG-AI-01}
{#SR-AGG-MEDIA-02}
