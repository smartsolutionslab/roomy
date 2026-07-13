# 0064. The Wolverine outbox adapter defers commit to an ambient message-handler transaction

- **Status:** Accepted
- **Date:** 2026-07-12
- **Deciders:** Heiko Weiß

## Context and problem statement

`application` owns an `IUnitOfWork` and an `IIntegrationEventOutbox` port (ADR-0005). The Wolverine
adapter `WolverineIntegrationEventOutbox` implements the outbox by enrolling the `DbContext` in
Wolverine's `IDbContextOutbox` and calling `SaveChangesAndFlushMessagesAsync`, which — per Wolverine —
saves the context, **commits the current transaction**, and flushes the staged messages:

```csharp
await ActiveContext.SaveChangesAsync(token);
if (ActiveContext.Database.CurrentTransaction != null)
    await ActiveContext.Database.CommitTransactionAsync(token);   // ← commits whatever transaction exists
await FlushOutgoingMessagesAsync();
```

For an **HTTP** command handler this is correct: there is no ambient transaction, `SaveChangesAsync`
auto-commits, and the outbox flushes — the outbox owns the unit of work.

For a **message consumer** it is wrong. Wolverine's `AutoApplyTransactions()` (ADR-0005 wiring) wraps
every consumer in generated middleware that **begins the `DbContext` transaction, then commits it and
flushes the outbox** once the handler returns. When a consumer-invoked command handler calls
`IUnitOfWork.SaveChangesAsync`, the adapter's `SaveChangesAndFlushMessagesAsync` commits *the
middleware's* transaction and closes the connection; the middleware's own `EfCoreEnvelopeTransaction.
CommitAsync` then runs against a dead connection and throws `InvalidOperationException: Connection is
not open`. The message retries five times and dead-letters.

This bites organization's `CompleteEmployeeProvisioning` and `FailEmployeeProvisioning` — the saga
steps that complete/fail the default-admin provisioning — because they are consumer-invoked yet write
through `IUnitOfWork`. Identity's and attendance's consumers avoid it only by accident: they publish
via `IIntegrationEventPublisher`/mutate the `DbContext` directly and never call the eager outbox, so
the middleware alone commits.

## Decision drivers

- **`IUnitOfWork.SaveChangesAsync` must be safe in both call sites.** The same application handler may
  be reached from an HTTP endpoint or a message consumer; the outbox must not double-commit.
- **The transaction owner commits once.** Under Wolverine's middleware the *middleware* owns the
  transaction and the outbox flush; outside it, the *adapter* does.
- **Keep the abstraction uniform.** Do not special-case individual handlers, and do not force
  consumer handlers onto a different publishing API — the split (some handlers use `IUnitOfWork`,
  others `IIntegrationEventPublisher`) is exactly what let this rot go unnoticed.
- **Deterministically testable** with Wolverine's in-memory harness, no broker or Postgres.

## Considered options

- **A — Align the two handlers.** Rewrite `CompleteEmployeeProvisioning`/`FailEmployeeProvisioning` to
  the publisher pattern so they never call the eager outbox. Minimal, but leaves the landmine armed:
  the next consumer that writes through `IUnitOfWork` reintroduces the bug, and the abstraction stays
  unsafe by contract.
- **B — Fix the adapter to defer to an ambient transaction (chosen).** `WolverineIntegrationEventOutbox`
  detects an ambient `DbContext` transaction. If one exists (a Wolverine handler), it enrolls the
  events on the ambient message bus and saves, but does **not** commit or flush — the middleware does.
  If none exists (HTTP), it keeps today's behaviour: enroll, publish, save-commit-flush through the
  `IDbContextOutbox`. `IUnitOfWork.SaveChangesAsync` becomes safe everywhere.
- **C — Disable `AutoApplyTransactions` and make the adapter the sole commit point everywhere.**
  Uniform, but larger blast radius: identity's and attendance's consumers rely on the middleware to
  save/commit and would each need to call the unit of work explicitly. More churn, more risk.

## Decision

**Option B.** `WolverineIntegrationEventOutbox` takes Wolverine's `IMessageBus` alongside
`IDbContextOutbox` and branches on `context.Database.CurrentTransaction`:

```csharp
if (context.Database.CurrentTransaction is not null)
{
    // Inside a Wolverine handler: AutoApplyTransactions owns the transaction and will save, commit,
    // and flush the outbox when the handler returns. Publishing on the ambient bus enrols the events
    // in that same transaction; committing or flushing here would close the connection out from under
    // the middleware's own commit.
    foreach (var integrationEvent in integrationEvents)
        await messageBus.PublishAsync(integrationEvent);
    await context.SaveChangesAsync(cancellationToken);
    return;
}

// No ambient transaction (an HTTP request): own the outbox — enrol, stage, save-commit-flush.
outbox.Enroll(context);
foreach (var integrationEvent in integrationEvents)
    await outbox.PublishAsync(integrationEvent);
await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
```

In a generated consumer Wolverine fulfils the `IMessageBus` dependency with the **ambient envelope
context**, so `PublishAsync` stages the events on the very context the middleware commits and flushes —
the same mechanism identity's `WolverineIntegrationEventPublisher` already relies on.

## Consequences

**Positive**
- `IUnitOfWork.SaveChangesAsync` is safe from HTTP endpoints and message consumers alike; the double
  commit and the `Connection is not open` dead-lettering are gone.
- The abstraction is uniform — no per-handler special cases, both publishing ports stay valid.
- The default-admin provisioning saga completes without retrying/dead-lettering.

**Negative / trade-offs**
- The adapter now depends on `IMessageBus` in addition to `IDbContextOutbox`; in the HTTP path the bus
  is resolved but unused. Acceptable — a scoped bus is cheap and the branch is explicit.
- The discriminator is "a transaction already exists," not "am I inside Wolverine." That holds because
  HTTP handlers are plain minimal APIs with no ambient transaction (verified) and Wolverine's
  middleware always begins one; if an HTTP path ever opens its own transaction it must own the commit,
  which is the correct semantics anyway.
- Consumers still **ignore the command `Result`** (unlike identity's, which throws on failure). That is
  a separate latent bug — a genuine domain failure is swallowed — and is left for a follow-up; this ADR
  only removes the transaction conflict.

**Follow-ups**
- Make `UserRegisteredConsumer`/`UserProvisioningFailedConsumer` act on the handler `Result` (throw to
  retry on transient failure) so real failures are not silently dropped.
- A real Postgres + broker outbox round-trip remains deferred to the Testcontainers suite (#68); the
  transaction-ownership contract is covered by the in-memory harness here.
