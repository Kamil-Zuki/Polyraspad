# Группа 10: LLM-провайдер (LLM Provider)

## Введение

Для explain_word и general_answer Agent Service вызывает **OpenAI-compatible Chat Completions API** через typed HttpClient. Конфигурация — секция `Ai` в appsettings / environment.

**Метафора:** LLM-провайдер — **кабина переводчика за стеклом**. Агент формулирует запрос, внешняя модель генерирует текст, а сервис фильтрует ответ до безопасного учебного формата.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к LLM-провайдер (LLM Provider).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-LLM-01** | **OpenAI-compatible completion:** POST chat/completions; ApiKey, Model, Timeout из AiOptions. |

---

# Детальная спецификация требований

## SR-AGENT-LLM-01: OpenAI-compatible completion {#SR-AGENT-LLM-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **BaseUrl** | Default `https://api.openai.com/v1`; override для proxy/Mistral-compatible. |
| **Enabled flag** | `Ai:Enabled=false` → model null on run, LLM paths may fail gracefully. |
| **Timeout** | Clamped 5..600 seconds on HttpClient. |
| **Implementation** | `OpenAiCompatibleAgentLlmProvider` — single user message prompt pattern. |

### 2. Высокоуровневое описание

Представим LLM-провайдер как **кабину переводчика за стеклом**.

1. **Scope boundary:** вызывается только из orchestrator learning tools (`explain_word`, `general_answer`) — не navigation/progress/out_of_scope.
2. **OpenAI-compatible:** POST `chat/completions` через typed HttpClient; BaseUrl override для proxy/Mistral-compatible endpoint.
3. **AiOptions:** ApiKey, Model, Timeout (clamp 5..600s); `Ai:Enabled=false` → model null, graceful failure paths в orchestrator.
4. **Single-message pattern:** `OpenAiCompatibleAgentLlmProvider` — один user prompt; output sanitizes downstream через `SanitizeLemmaLabels`.

Таким образом, внешняя модель генерирует текст только для допустимых учебных инструментов с configurable provider boundary.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: LLM timeout (Negative Path)

1. Provider timeout.
2. Orchestrator catch → assistant «Something went wrong.» + tool failed.

---

*Следующая группа: [[11 - Платформенные контракты (Operations)]].*
