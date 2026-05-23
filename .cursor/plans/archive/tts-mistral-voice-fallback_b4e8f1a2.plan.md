---
name: tts-mistral-voice-fallback
overview: When Mistral is used for LLM but no valid Mistral TTS voice_id is configured, auto TTS falls back to espeak instead of failing Create card / Reader Listen.
todos:
  - id: backend-auto-fallback
    content: Resolve TTS auto provider to espeak when Mistral base URL lacks valid voice_id.
    status: completed
  - id: docker-default-espeak
    content: Default AI_TTS_PROVIDER to espeak in docker-compose when unset.
    status: completed
  - id: tests-and-frontend-hint
    content: Add regression tests and clearer frontend error hint for voice_id config.
    status: completed
  - id: verify-backend-tests
    content: Run AggregatorService.Tests TtsSpeechClientTests.
    status: completed
isProject: false
---

# TTS Mistral voice_id fallback

## Goal
Create card / Generate audio works without requiring manual Mistral voice_id when only Mistral LLM URL is configured.

## Fix
- `auto`: Mistral TTS only if `AI_TTS_VOICE_ID` (or per-lang voice) is a real Mistral voice_id
- Otherwise fallback to `espeak` (Docker-friendly)
- Explicit `AI_TTS_PROVIDER=mistral` without voice still returns clear error
