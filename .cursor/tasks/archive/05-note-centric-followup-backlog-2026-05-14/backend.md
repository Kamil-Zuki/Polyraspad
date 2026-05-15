# backend-agent Task

Plan ID: `05-note-centric-followup-backlog-2026-05-14`
Agent: `backend-agent`
Status: done
Can run in parallel: no

## Objective
Ship P1 preview fixes, P2 optional capture deck, P3 study DTO derivation rules, gRPC/REST contract alignment.

## Deliverables
- `CommunityService.GetProductDetailsAsync`: Include `Deck` + `Note` on marketplace preview cards.
- `CardGrpcService.CheckCardDuplicates`: map `CardDuplicatePreviewDto` → `CardPreview` via AutoMapper.
- Proto + services: optional `deck_id` / `DeckId` on capture; `CaptureCardAsync` resolves owner deck or Inbox.
- Validators + `ArgumentException` → `InvalidArgument` on bad deck.
- **P3:** `StudyService.GetNextCardAsync`: `Content.TargetLemma` = surface Word from note (term-first), fallback `Lemma?.Text` only when Word empty; XML/proto comments on `CardStudyDto`; `StudyServiceGetNextCardNoteDerivedContentTests`.

## Verification
- `dotnet build` VocabularyService, AggregatorService
- `dotnet test VocabularyService.Tests --filter "FullyQualifiedName~CardService"`
- `dotnet test VocabularyService.Tests --filter "FullyQualifiedName~StudyService"`

## Handoff
P1–P3 slices delivered 2026-05-14. **P4 (2026-05-14):** `CardService` term-first create/update/bulk/read paths; `StudyService` sibling bury + sibling counts via `GetSiblingSessionRedisMember` (`L:` / `T:`). P5 proto hygiene remains.
