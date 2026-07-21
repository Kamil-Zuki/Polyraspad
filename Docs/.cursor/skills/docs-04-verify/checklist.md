# Verify checklist

Mark each item **pass** / **fail**. On fail → ISSUE.

## A — Upstream readiness (`03` ↔ `01` ↔ `04`)

- [ ] **01↔03:** every SR in `01` that touches persistence references entities/fields present in `03`
- [ ] **01↔03:** in-scope entities in `03` have supporting SR(s) in `01` (or documented out-of-scope)
- [ ] **01↔03:** no naming/cardinality conflict between SR text and entity docs — or open ISSUE
- [ ] `03` entities cover fields referenced in DTO/proto tables
- [ ] SR codes in `04` tables exist in `01`
- [ ] Group file names in `04` match `01` capability groups (order and titles)

## B — gRPC + proto

- [ ] Every `rpc` in `.proto` has markdown row + `#grpc-MethodName` anchor
- [ ] Every gRPC markdown RPC has matching `rpc` in proto (or documented exception in ISSUE)
- [ ] Enum first value `_UNSPECIFIED = 0`
- [ ] gRPC status tables present for each RPC block

## C — DTO

- [ ] Each DTO block has `#dto-Name` anchor
- [ ] Fields trace to `03` (entity.field) or marked as computed/gateway-only with rationale
- [ ] JSON examples match structure table

## D — REST API

- [ ] Each endpoint links to `#grpc-MethodName`
- [ ] No heavy business logic on BFF without naming delegated gRPC
- [ ] HTTP error tables present
- [ ] DTO links use `#dto-*` where applicable

## E — Socket

- [ ] Each event documents triggering gRPC method(s)
- [ ] Metadata table complete (direction, auth, payload DTO)

## F — Integrations

- [ ] Outbound HTTP/gRPC only (Rabbit patterns documented under Rabbit MQ, not here)
- [ ] Auth, methods, errors sections present per integration file

## G — Rabbit MQ

- [ ] Exchange, routing key, queue, payload documented
- [ ] ACK/NACK/DLQ behavior stated
- [ ] Aligns with `02` КАР for messaging patterns

## H — Redis

- [ ] Key patterns and TTL documented
- [ ] Fail-open / fail-closed stated
- [ ] Aligns with `02` КАР

## I — Algorithms

- [ ] I/O tables + pseudocode
- [ ] Links to gRPC / Redis / Rabbit / relevant КАР

## J — Staging hygiene

- [ ] `00 - Реестр проблем.md` lists all open ISSUEs
- [ ] No duplicate ISSUE for same root cause
