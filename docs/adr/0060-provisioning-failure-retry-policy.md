# 0060. Retry transient provisioning failures instead of swallowing them

- **Status:** Accepted
- **Date:** 2026-06-12
- **Deciders:** Heiko Weiß

## Context and problem statement

The User↔Employee provisioning saga (ADR-0025) has the organization context emit `EmployeeHired`,
which the identity context consumes (`EmployeeHiredConsumer`) and turns into a `RegisterUser`
command that provisions the Keycloak account.

Two defects made a failed provisioning step disappear silently:

1. **`EmployeeHiredConsumer` ignored the `RegisterUser` result.** A failure returned by the use case
   was discarded, so Wolverine considered the message handled and dropped it — no retry, no
   dead-letter.
2. **`RegisterUserHandler` treated every failure the same.** Both a *terminal* failure (the work
   email is already taken, the password is rejected) and a *transient* one (the credential provider
   is unreachable / still warming up at startup) published `UserProvisioningFailed` and returned an
   error.

Combined, a transient provider outage at startup — common because the seeded `DefaultAdmin` is
provisioned while Keycloak may still be warming up (ADR-0025 amendment) — left the employee stuck in
*provisioning* forever, with no account and no trace. This was observed in practice: the admin
`Employee` existed in *provisioning*, but no identity `User`, no Keycloak user, and an empty
dead-letter table.

## Decision drivers

- A cross-service saga step must never be silently lost (ADR-0014: integrate only via durable
  events; ADR-0005: outbox/inbox).
- `008-hire-employee` FR-008: transient failures are retried until they converge; FR-007: a terminal
  failure marks the employee *failed* (no half-accounts).
- Retries must be bounded and end in an observable dead-letter, not an infinite loop.
- The policy is cross-cutting (it affects every consumer), so it belongs in the shared messaging
  composition, not in one context.

## Decision

**Distinguish terminal from transient provisioning failures, and retry the transient ones.**

- **`RegisterUserHandler`** now classifies the provider error. A *terminal* error (`email_taken`,
  `password_rejected`) compensates the saga as before — it publishes `UserProvisioningFailed` and
  returns **success** (the message is fully handled; do not retry). A *transient* error (anything
  else, e.g. `provider_error`) returns a **failure** result and does **not** compensate, signalling
  that the message should be retried.
- **`EmployeeHiredConsumer`** no longer ignores the result: a failure is surfaced as an exception so
  Wolverine's retry machinery engages.
- **The shared messaging setup** (`MessagingServiceCollectionExtensions`) adds a global
  `OnException<Exception>().RetryWithCooldown(1s, 5s, 15s, 30s, 60s)`. A failing consumer is retried
  with a widening cooldown (~110s total) and then moved to the dead-letter queue. The durable inbox
  keeps the retries idempotent.

This rides out a downstream that is briefly unavailable (the startup-warmup race) while keeping a
permanently-bad message bounded and visible in the dead-letter queue.

## Consequences

**Positive**
- A transient provider outage no longer loses the saga step; provisioning converges once the
  provider is reachable. The seeded admin self-heals together with the startup re-drive (ADR-0025
  amendment, `specs/021-resilient-admin-provisioning`).
- Failures are never silent: either they retry to success, or they land in the dead-letter queue.
- The retry policy is global and transport-agnostic, so it covers every consumer uniformly.

**Negative / trade-offs**
- A genuinely poison message is now retried five times over ~110s before dead-lettering, adding
  latency to its failure surfacing (acceptable — it still ends in the dead-letter queue).
- "Terminal failure returns success" overloads the use-case result to mean *message handled*
  (provisioned **or** compensated) rather than *provisioning succeeded*; the distinction is captured
  by the published `UserRegistered` vs `UserProvisioningFailed` events and covered by tests.
- A transient failure that exhausts its retries leaves the employee in *provisioning* (not *failed*);
  for the seeded admin this is intentional (the startup re-drive retries it), and for an interactive
  hire the dead-letter entry makes it observable. Compensating an exhausted transient failure is a
  possible future refinement.

## Follow-ups

- Consider compensating (mark *failed*) when a transient provisioning failure exhausts its retries
  and dead-letters, to fully satisfy `008` FR-007 for interactive hires.
