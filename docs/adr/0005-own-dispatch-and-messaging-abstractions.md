# 0005. Own the dispatch and messaging abstractions; defer Wolverine to the edge

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

We want Roomy's core (`domain` and `application`) independent of any framework, per the
Clean Architecture stance in ADR-0003. Mediator and messaging frameworks are the usual
place that independence breaks — teams inject `IMediator` or a concrete message bus into
the application layer, and the core silently becomes coupled to it. We also do not need
async messaging or a transactional outbox on day one; introducing that machinery early
adds reliability and operational concerns before any feature requires them.

## Decision drivers

- Keep `domain`/`application` free of framework types; unit-testable without a bus.
- Defer costly infrastructure (messaging, outbox/inbox) until a feature genuinely needs
  it — avoid premature distribution.
- Preserve optionality: the messaging implementation should be swappable.
- Avoid a hard dependency on MediatR (a framework dependency in the core, with its own
  licensing considerations).

## Considered options

- **Own thin dispatch/messaging abstractions; Wolverine as a deferred infrastructure
  adapter.**
- Inject Wolverine's bus directly into the application layer — simplest, but couples the
  core to Wolverine.
- Use MediatR as the in-core dispatcher — still a framework dependency in the core.

## Decision

The `application` layer defines its own contracts: command/query handler abstractions
and an outbound integration-event port (e.g. `IIntegrationEventPublisher`). In-process
handling requires no framework initially. **Wolverine is introduced only when async
messaging or the transactional outbox/inbox is actually required** (cross-context
integration, delivery reliability). At that point Wolverine implements the owned ports
in `infrastructure` and is configured at the composition root. `domain` and
`application` never reference Wolverine or any other framework type.

This supersedes the earlier framing of "Wolverine as the dispatcher."

## Consequences

**Positive**
- The core stays framework-free and testable without infrastructure.
- Wolverine's strong outbox/inbox and messaging remain available when justified, without
  polluting the core.
- Messaging is deferred until a feature needs it; the implementation can be swapped.

**Negative / trade-offs**
- Some mapping/indirection between the owned ports and Wolverine.
- Risk of re-implementing what Wolverine already provides if the abstractions are
  over-built — keep them thin and add only what a current feature needs (simplicity
  first); do not abstract speculatively.

**Follow-ups**
- Define the minimal handler/dispatch contracts and the integration-event port in
  `application`.
- Add a NetArchTest rule forbidding framework references (Wolverine, MediatR) in
  `domain` and `application`.
