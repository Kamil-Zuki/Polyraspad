# Введение

Токенизация/лемматизация текста и математика **FSRS**-расписания вынесены в Python-микросервис **inclusive** (gRPC `:40051`), а не реализованы внутри .NET Vocabulary.

## Контекст и проблема

* FSRS-библиотека и NLTK удобнее в Python-экосистеме.
* Смешение NLP/scheduling с EF/CRUD в одном процессе усложняет деплой и тестирование.
* Нужен стабильный контракт (`vocab.proto`) для `AnalyzeText` / `ReviewCard`.

## Принятое решение

1. Vocabulary вызывает inclusive по gRPC для NLP и FSRS review.
2. Результаты (токены, даты due, stability и т.д.) применяются к доменным сущностям Vocabulary (cards, study progress).
3. Term-first политика остаётся на стороне Vocabulary: inclusive не становится источником статуса знания.

## Обоснование и последствия

* Независимый scale/restart inclusive.
* Контрактные pytest (`test_fsrs_review_card.py`) рядом с Python-кодом.
* .NET фокусируется на персистентности, правах и SR-VOC бизнес-правилах.
