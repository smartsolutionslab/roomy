# 0051. Application use cases split into Commands/ and Queries/ with a Handlers/ subfolder

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

Each context's `application` library puts every use case in one flat `UseCases/` folder. In attendance
that folder holds 23 files of four different kinds with no separation: command messages (`ReservePlace`),
query messages (`ViewOccupancy`), their handlers (`ReservePlaceHandler`, `ViewOccupancyHandler`), and the
result/view records those use cases return (`ReservationView`, `OccupancyView`, `BookableRoomView`, …). A
reader cannot tell a write from a read, a message from its handler, or a request shape from a result shape
without opening each file and checking the marker interface.

The CQRS split is already explicit in the type system — `ICommand`/`ICommandHandler` vs
`IQuery`/`IQueryHandler` (`libs/application-contracts/Messaging`) — but the folder structure throws that
information away. The repo's folder=namespace convention (ADR-0049/0050) means the structure should carry
it.

How should an `application` library organise its use cases so the read/write split and the
message/handler split are visible in the folder tree?

## Decision drivers

- **CQRS visible in the tree.** Commands and queries are different shapes with different rules (one
  mutates an aggregate, the other reads a model); the folders should say so.
- **Message vs handler separation.** The message is the contract (often referenced by endpoints, tests,
  the DI registration); the handler is the implementation. Nesting handlers keeps the folder you open to
  learn "what can this context do?" free of implementation noise.
- **Folder = namespace.** Consistent with the rest of the codebase (ADR-0049/0050).
- **Result shapes belong to their use case.** A query's view record and a command's result are part of
  that use case's contract, read together with the message — they should not scatter.

## Considered options

- **A — Keep one flat `UseCases/`.** Lowest churn; the four kinds stay intermixed.
- **B — `Commands/` and `Queries/`, handlers alongside their message.** Splits read from write but leaves
  message and handler in the same folder.
- **C — `Commands/` and `Queries/`, each with a `Handlers/` subfolder (chosen).** Splits read from write
  *and* contract from implementation. Result/view records sit with their message.

## Decision

**Option C.** In every context's `application` library, replace `UseCases/` with:

```
application/
├─ Commands/            # ICommand messages + their result records      → …Application.Commands
│  └─ Handlers/         # the ICommandHandler implementations           → …Application.Commands.Handlers
└─ Queries/             # IQuery messages, query-input VOs + view records → …Application.Queries
   └─ Handlers/         # the IQueryHandler implementations              → …Application.Queries.Handlers
```

- **`Commands/`** holds the `ICommand`/`ICommand<T>` message records and any command *result* record
  (e.g. `HiredEmployee`, returned by `HireEmployee`). **`Commands/Handlers/`** holds the matching
  `ICommandHandler` types.
- **`Queries/`** holds the `IQuery<T>` message records, any query-input value object (e.g.
  `OccupancyScope`), and the *view/result* records the queries return (`BookableRoomView`,
  `OccupancyView`, `ReservationView`, …). **`Queries/Handlers/`** holds the matching `IQueryHandler`
  types.
- The handler subfolder is **`Handlers/`** (plural), matching the repo's plural-collection folder
  convention (`ReadModels/`, `Employees/`).
- A context with no queries (identity, organization today) has no `Queries/` folder; one with no commands
  would have no `Commands/`. The folders follow the use cases that exist, not a fixed template.

Namespaces follow the folders: `…Application.Commands`, `…Application.Commands.Handlers`,
`…Application.Queries`, `…Application.Queries.Handlers`. A handler references its message/result via a
`using` of the parent (`…Commands` / `…Queries`); the explicit handler registrations in each
`*InfrastructureServiceCollectionExtensions` reference both namespaces.

## Consequences

**Positive**
- "What can this context do?" is two folders — `Commands/` and `Queries/` — read without handler noise.
- The read/write split and the message/handler split are both visible in the tree and the namespace.
- Each use case's request and result shapes stay together.

**Negative / trade-offs**
- One-off churn: every consumer of a use-case type swaps `using …Application.UseCases;` for the
  `…Application.Commands` and/or `…Application.Queries` namespace; handlers and the DI registration gain a
  `using` for the parent message namespace. Mechanical, and the warnings-as-errors build enforces it.
- The split is structural only — no behaviour, no contract, no OpenAPI/spec change.

**Follow-ups**
- csharp.md and CLAUDE.md record the rule; new use cases place their files accordingly.
