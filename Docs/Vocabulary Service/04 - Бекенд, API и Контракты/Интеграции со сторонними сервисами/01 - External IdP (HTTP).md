# Интеграция с Python-микросервисом `inclusive`

Данный документ описывает gRPC-интеграцию `VocabularyService` с Python-микросервисом `inclusive` (порт **40051**).

---

## 1. Назначение интеграции

`VocabularyService` делегирует ресурсоемкие NLP и FSRS математические операции в Python-микросервис `inclusive`:
1. **FSRS Scheduling:** Расчет новых значений `Stability`, `Difficulty` и даты `Due` при вызове `ReviewCard`.
2. **NLTK Tokenization:** Разбиение входного текста на токены слов и знаки препинания с лемматизацией в NLTK.

---

## 2. Protobuf-контракт (`vocab.proto`)

```protobuf
syntax = "proto3";
package pvs.vocab.v1;

service VocabService {
  rpc ReviewCard (ReviewCardRequest) returns (ReviewCardResponse);
  rpc TokenizeText (TokenizeTextRequest) returns (TokenizeTextResponse);
}
```

### Вызов `ReviewCard`
- **Запрос:** `card_id`, `rating` (1=Again, 2=Hard, 3=Good, 4=Easy), текущие `stability`, `difficulty`, `reps`, `lapses`, `elapsed_days`.
- **Ответ:** новые `stability`, `difficulty`, `scheduled_days`, `state` (New=0, Learning=1, Review=2, Relearning=4).
