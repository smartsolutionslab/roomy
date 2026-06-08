# 0028. Defer Nx Cloud remote caching

- **Status:** Accepted
- **Date:** 2026-06-08
- **Deciders:** Heiko Weiß

## Context and problem statement

The workspace is an Nx monorepo (ADR-0002) with local computation caching already enabled
(`targetDefaults` mark `build`, `lint`, `test`, and the Angular executors as cacheable).
Nx Cloud offers **remote caching** — sharing task-output artifacts across machines and CI
runs — plus distributed task execution, run insights, and GitHub PR integration. The
workspace was scaffolded with `nxCloud=skip`, so it is not connected.

The question: connect to Nx Cloud for remote caching now, or defer? Per golden rule 4 this
is a cross-cutting infrastructure decision, so it is recorded here before any wiring.

## Decision drivers

- **Team shape.** Solo developer plus AI agents on one primary machine. Remote caching pays
  off most when many developers and CI workers re-run the same tasks; a single-machine team
  already gets the win from the **local** cache.
- **Data governance.** Remote caching uploads task **inputs (hashes) and outputs (build
  artifacts, logs)** to a third-party SaaS. For a B2B product this is an external-data-sharing
  decision that deserves a deliberate review, not a default opt-in. Sending build artifacts
  off-box is hard to walk back once cached.
- **Cost and lock-in.** Nx Cloud is a paid service beyond a free tier; adopting it couples
  CI economics to a vendor.
- **Reversibility.** Connecting later is a small, additive step (`nx connect`); deferring
  costs almost nothing.

## Considered options

- **Connect Nx Cloud now** — immediate remote caching and CI integration, at the cost of
  vendor coupling, spend, and uploading build artifacts before there is a CI fleet to
  benefit.
- **Defer; keep the local cache** — no remote caching yet; revisit when the team or CI scale
  makes it worthwhile and after a data-governance review.
- **Self-hosted remote cache** (custom Nx remote-cache backend, e.g. an S3/GCS bucket) —
  keeps artifacts in our own infrastructure; more setup, revisit if/when remote caching is
  warranted but third-party hosting is not.

## Decision

**Defer Nx Cloud.** The local Nx cache already covers the single-machine workflow, and the
marginal benefit of remote caching is low until there is a shared CI fleet or additional
contributors. We will not upload build artifacts to a third-party service until there is a
concrete need and a data-governance check. The `nx init` step from Nx's onboarding is **not
applicable** — Nx is already installed and configured; only the Nx Cloud connection was ever
in question.

## Consequences

**Positive**
- No new vendor dependency, spend, or external artifact upload now.
- Local caching keeps giving fast incremental builds/tests.
- The decision and its revisit trigger are recorded; connecting later stays a one-step,
  additive change.

**Negative / trade-offs**
- No cross-run/CI cache sharing yet — acceptable while CI is light and single-origin.
- When CI parallelism grows, cold caches on CI runners cost time until this is revisited.

**Follow-ups**
- **Revisit trigger:** a second regular contributor joins, or CI runtime/parallelism grows
  enough that cold caches hurt. At that point, weigh **Nx Cloud** against a **self-hosted
  remote cache**, and complete a data-governance review of what artifacts would leave our
  infrastructure before connecting.
- If adopted later, connect via `nx connect` (no `nx init`) and record the outcome by
  superseding this ADR.
