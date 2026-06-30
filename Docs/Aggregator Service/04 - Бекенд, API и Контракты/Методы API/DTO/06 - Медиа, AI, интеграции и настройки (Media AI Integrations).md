# 06 - Медиа, AI, интеграции и настройки (Media AI Integrations)

## Media

`UploadImageResponseDto`, `UploadDocumentResponseDto`, `ExtractDocumentTextResponseDto`, `GenerateAudioDtos` — см. корень `Dtos/` и Media REST [[09 - Медиа и Reader Library (Media)]].

## AI Proxy {#dto-AiGenerateRequestDto}

См. `AiMiningDraftDtos.cs` и AI controller models — `AiGenerateRequestDto`, `AiGenerateResponseDto`, `MiningDraftRequestDto`, `MiningDraftResponseDto`.

## Integrations {#dto-TranslateRequestDto}

См. `IntegrationDtos.cs`:

| DTO | Назначение |
| :--- | :--- |
| TranslateRequestDto | text, sourceLang, targetLang, provider? |
| TranslateResponseDto | translatedText, provider, source |
| DictionaryEntryDto | term, definitions[] |

## User settings {#dto-UserSettingsDto}

### UserSettingsResponseDto {#dto-UserSettingsResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| markRemainingKnownOnPageTurn | bool | Reader: синие → known при перелистывании |
| dailyGoalMinutes | int? | |
| preferredTtsVoice | string? | |
| timezoneOffsetMinutes | int? | Analytics daily |

### UpdateUserSettingsDto {#dto-UpdateUserSettingsDto}

Partial update — те же поля optional.
