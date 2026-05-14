# Frontend Task

Plan ID: `editor-inoriginal-fields-2026-05-14`
Agent: `frontend-agent`
Status: done
Can run in parallel: partial — макет/компоненты можно крутить локально, но submit заблокирован до финального DTO если нужны новые поля API

## Objective

Обновить `/editor`: `EditorForm`, `EditorCardContext`, `CardPreview`, гидратацию при `?cardId=`, чтобы поддерживать набор полей как в `CaptureApp` sidepanel (сетка `editor-grid`): Expression, Word, Transcription, Word Types, Translation, Definition, Example/Context, Synonyms, Antonyms, Source, Url — плюс паттерны WordPicker / Use selected word / Define word, согласованные с доступными API PVS (dictionary/AI).

## Inputs

- Plan: `.cursor/plans/archive/editor-inoriginal-fields-2026-05-14.md`
- Референс UX: `inoriginal-capture-extension/src/ui/CaptureApp.tsx`
- Текущее: `polyraspad-frontend/src/components/editor/editor-form.tsx`, `editor-card-context.tsx`, `editor-card-hydrator` (если есть)

## Scope

- Расширить состояние редактора и форму; не ломать существующие сценарии загрузки изображения/аудио и source meta без явного решения product.
- Тесты: обновить/добавить для формы и страницы editor при изменении полей.

## Out of Scope

- Портирование waveform и capture pipeline из расширения.

## Deliverables

- Изменения в коде + тесты; выравнивание `types.ts` с backend DTO.

## Verification

- `npm test -- --testPathPattern=editor` (или существующие пути к тестам editor).

## Handoff

- Список файлов; поведение при старых карточках без новых полей.
