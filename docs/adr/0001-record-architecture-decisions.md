# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy is built primarily by AI agents under human direction. Agents act on the context
they are given; when the reasoning behind a structural choice is not written down, that
reasoning is lost between sessions and decisions get silently re-litigated or violated.
We need a durable, low-friction way to capture significant decisions so both humans and
agents inherit the *why*.

## Decision drivers

- Continuity across agent sessions, which have no shared memory of past reasoning.
- Low friction — heavyweight process will not be kept up.
- Reviewable in the same flow as code (lives in the repo, goes through PR).

## Considered options

- Architecture Decision Records (MADR) in the repo.
- A wiki / external knowledge base.
- Comments in code and commit messages only.

## Decision

We use **Architecture Decision Records in MADR format**, stored in `docs/adr/`,
versioned with the code and reviewed via pull request. ADRs are written before the
implementing change. Accepted ADRs are immutable; a decision is changed by superseding
it with a new ADR.

## Consequences

**Positive**
- Agents can be pointed at `docs/adr/` for authoritative rationale.
- Decisions and their trade-offs are discoverable and diff-able.
- Reversal cost and reasoning are explicit.

**Negative / trade-offs**
- A small, ongoing discipline cost per significant decision.

**Follow-ups**
- Backfill the decisions already made (0002–0008).
- Reference `docs/adr/` from `CLAUDE.md` so agents read it by default.
