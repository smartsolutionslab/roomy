# 0061. Per-service listener queues for fan-out integration events

- **Status:** Accepted
- **Date:** 2026-06-12
- **Deciders:** Heiko Weiß

## Context and problem statement

Integration events are published once and consumed by every interested context (pub/sub, ADR-0005/0031).
The messaging composition uses Wolverine's RabbitMQ **conventional routing**, which, by default, names a
listener's queue after the **message type alone** (`type.ToMessageTypeName()`).

When two contexts subscribe to the **same** event, that default makes them declare a queue with the
**same name** — so they share a single queue and **compete** for messages (RabbitMQ round-robins each
message to exactly one consumer) instead of each receiving a copy. The reaction of whichever consumer
does *not* receive the message is silently skipped.

This was a latent defect with one victim: `EmployeeHired` is consumed by **identity** (provision the
Keycloak account) *and* **attendance** (add the employee to the occupancy directory). They shared one
`…EmployeeHired` queue. The only event that actually flows through this saga at runtime is the seeded
`DefaultAdmin`'s hire (the demo employees are seeded directly, bypassing messaging), so when attendance
won that single message, the admin's credential was never provisioned and the admin could not log in
(#189). The saga-e2e test did not catch it because its test apphost has no attendance-api, so there was
no competing consumer.

## Decision drivers

- Integration events are pub/sub: every subscribing context must receive its **own copy** (ADR-0005).
- The fix must be transport-level and generic — it belongs in the shared messaging composition, not in
  any context.
- Queue names must be **stable** across restarts so the durable queue persists (no orphaning).

## Decision

**Name each listener queue per service.** Keep the conventional **exchange** (sender identifier) named
after the message type — a single fanout exchange per event — but override the **listener queue** name to
include the service:

```
QueueNameForListener(messageType => $"{messageType.ToMessageTypeName()}.{serviceName}")
```

`serviceName` is the host's application-assembly name (the same assembly already pinned as Wolverine's
`ApplicationAssembly`), falling back to `WolverineOptions.ServiceName`. Each subscribing service therefore
declares its own queue (e.g. `…EmployeeHired.Roomy.Identity.Api`, `…EmployeeHired.Roomy.Attendance.Api`),
each bound to the shared `…EmployeeHired` fanout exchange, so the broker delivers a copy to **every**
subscriber.

## Consequences

**Positive**
- True pub/sub fan-out: independent subscribers of one event no longer compete; each reacts to every
  occurrence. The seeded admin now provisions on first boot and can sign in (#189, verified end to end).
- Generic and transport-level — applies to every event and every service uniformly; no per-context code.
- Stable queue names (derived from the assembly name) survive restarts, so durable queues persist.

**Negative / trade-offs**
- The wire topology changes: queues are renamed from `<event>` to `<event>.<service>`. On an existing
  broker the old shared queues are abandoned (harmless in dev where the broker is ephemeral; a deploy
  would leave them empty until removed).
- Queue names are longer; well within RabbitMQ's 255-char limit.

## Related

- Complements the AppHost change for #189: organization-api now `WaitFor(keycloak)` so the startup
  `DefaultAdminSeeder` does not publish `EmployeeHired` before identity has declared its (now per-service)
  queue binding — otherwise the fanout exchange drops the message (publish-before-bind).
- ADR-0060 (provisioning retry), ADR-0025 (provisioning saga), `specs/021-resilient-admin-provisioning`.
