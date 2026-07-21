# Write order (folder 04)

Dependency order — **never** write REST/Socket before gRPC anchors exist.

```
1. gRPC/00 + gRPC group files + {service}_service.proto
2. DTO/00 + DTO group files
3. REST API/00 + group files     (skip if gRPC-only)
4. Socket/00 + group files       (skip if no WebSocket)
5. Integrations/00 + files
6. Rabbit MQ/00 + group files
7. Redis/00 + group files
8. Algorithms/00 + group files
```

Within each group file:

1. `# Введение`
2. `# 1. Список …` (summary table)
3. Detail blocks separated by `---`

## Per-layer constraints

| Layer | Owner | Writer must |
| :--- | :--- | :--- |
| gRPC | Microservice | Define RPC names, messages, status codes, `#grpc-*` |
| DTO | Gateway JSON | Link fields to `03`; `#dto-*` anchors |
| REST | BFF | Thin BFF logic + link to `#grpc-*` |
| Socket | BFF | Event metadata + gRPC trigger + optional REST refresh |
| Rabbit | Microservice | Exchange, queue, routing, payload, ACK/DLQ |
| Redis | Microservice | Key pattern, TTL, fail-open/closed |
| Algorithms | Microservice | I/O tables, pseudocode, links to gRPC/Redis/Rabbit/КАР |

## Batch size

Prefer **one group file** or **one `00` index** per writer invocation — keeps context focused and eases verify passes.
