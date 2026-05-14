# Product Task

Plan ID: `editor-inoriginal-fields-2026-05-14`
Agent: `product-agent`
Status: done
Can run in parallel: yes (перед финализацией контрактов backend/frontend)

## Objective

Зафиксировать продуктовую модель полей редактора, эквивалентную «черновику» InOriginal (`SentenceDraft`), в терминах PVS: обязательность, отображение в превью и в study, связь с `targetWord`/термином.

## Inputs

- Plan: `.cursor/plans/archive/editor-inoriginal-fields-2026-05-14.md`
- Референс: `inoriginal-capture-extension/src/shared/types.ts` (`SentenceDraft`)
- Текущий API: `polyraspad-frontend/src/lib/api/types.ts` — `CreateCardDto`, `UpdateCardDto`, `SourceMetaDto`

## Scope

- Таблица соответствия: поле расширения → поле PVS / хранение.
- Решение по **Source** vs **SourceMeta.title** и **Url** vs **SourceMeta.url** (объединить или держать дубли с синхронизацией).
- Нужен ли отдельный **Mnemonic** в v1 или отложить.

## Out of Scope

- Детальный UI-копипаст стилей расширения (светлая тема vs PVS dark).

## Deliverables

- Краткий spec в ответе lead-agent и/или обновление плана «Contracts To Lock».

## Verification

- Согласованность с term-first: целевое слово остаётся точной формой; не вводим леммы как источник истины.

## Handoff

- Явный список полей для DTO и для UI; blockers для backend.
