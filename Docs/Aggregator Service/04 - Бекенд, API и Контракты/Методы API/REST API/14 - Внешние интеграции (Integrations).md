# Введение



Outbound HTTP к MyMemory (translate) и Free Dictionary API (lookup). JWT. Статический список providers на BFF.



DTO: [[06 - Медиа, AI, интеграции и настройки (Media AI Integrations)]].



# 1. Список эндпоинтов



Сверено с `AggregatorService/Controllers/IntegrationController.cs`.



| SR | Method | Route | Назначение |

| :--- | :--- | :--- | :--- |

| SR-AGG-INT-01 | GET | `/api/integrations/providers` | translators + dictionaries |

| SR-AGG-INT-01 | POST | `/api/integrations/translate` | MyMemory translate |

| SR-AGG-INT-01 | POST | `/api/integrations/dictionary/lookup` | Free Dictionary lookup |



---



# SR-AGG-INT-01: Providers: GET /api/integrations/providers



## Общая информация



Статический список: `mymemory`, `freedictionary`.



| Тип метода | GET |

| :--- | :--- |

| **DTO запроса** | N/A |

| **DTO успешного ответа** | IntegrationProvidersResponseDto |



## Логика обработки запроса



* In-memory response, без external call



## Успешный ответ



HTTP **200**, списки `translators[]`, `dictionaries[]`.



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **401** | JWT |



---



# SR-AGG-INT-01: Translate: POST /api/integrations/translate



## Общая информация



On-demand перевод через MyMemory.



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | TranslateRequestDto (`text`, `sourceLanguage`, `targetLanguage`, `provider: "mymemory"`) |

| **DTO успешного ответа** | TranslateResponseDto |



## Параметры URL



Параметры отсутствуют.



## Логика обработки запроса



* Validate text non-empty, provider = `mymemory`

* HTTP GET к `api.mymemory.translated.net`

* Map response → `TranslateResponseDto`



## Успешный ответ



HTTP **200**, `{ provider, translatedText }`.



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **400** | Empty text / unsupported provider |

| **401** | JWT |

| **502** | Provider error |



---



# SR-AGG-INT-01: Dictionary lookup: POST /api/integrations/dictionary/lookup



## Общая информация



Lookup определения по **exact word form** (без лемматизации на BFF).



| Тип метода | POST |

| :--- | :--- |

| **DTO запроса** | DictionaryLookupRequestDto (`word`, `language`, `provider: "freedictionary"`) |

| **DTO успешного ответа** | DictionaryLookupResponseDto |



## Параметры URL



Параметры отсутствуют.



## Логика обработки запроса



* Validate word, provider = `freedictionary`

* HTTP GET к `api.dictionaryapi.dev/api/v2/entries/{lang}/{word}`



## Успешный ответ



HTTP **200**, word, phonetic, meanings[].



## Ошибки



| Статус-код | Описание |

| :--- | :--- |

| **400** | Empty word / unsupported provider |

| **404** | Word not found |

| **502** | Provider / parse error |


