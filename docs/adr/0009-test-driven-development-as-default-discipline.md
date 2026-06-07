# 0009. Test-driven development as the default implementation discipline

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy is built primarily by AI agents. The dominant failure mode in agent-driven
development is confidently producing wrong code against a misread requirement — code
that compiles, looks plausible, and passes review precisely because no one reads every
line. Verification therefore cannot rely on human line-by-line review or on the agent's
own claim that a task is "done." We need an objective, agent-proof signal of correctness
that is defined before implementation and cannot be bluffed past.

## Decision drivers

- An objective "done" signal the agent cannot fake (a failing test that must go green).
- A regression net that protects the domain as agents refactor and extend it.
- Design pressure: writing the test first forces the use case and the domain API to be
  defined from the caller's side before the implementation exists.
- Alignment with the "goal-driven execution" guardrail (criteria first, loop to green).

## Considered options

- **Test-first (TDD), red-green-refactor**, as the default discipline.
- Test-after — implement, then add tests.
- Acceptance tests from the spec only, no lower-level test discipline.

## Decision

**TDD is the default.** Each acceptance criterion is translated into a failing test
before any implementing code (Red), the minimum code is written to pass it (Green), then
the code is cleaned up under a green bar (Refactor). The discipline applies most strictly
to the `domain` and `application` layers, where logic lives and tests are fast and pure;
adapters in `infrastructure` are covered by integration tests (Testcontainers for real
dependencies). The Definition of Done requires that every acceptance criterion has a
test written before its implementation that now passes.

## Consequences

**Positive**
- The failing test is an agent-proof success criterion, cutting "confident but wrong"
  output at the source.
- A durable regression suite makes agent-driven refactoring safe.
- Test-first pressure improves the shape of the domain and application APIs.
- Fast feedback at the domain/application layer where tests are pure and quick.

**Negative / trade-offs**
- A discipline cost and slower per-step pace than implement-then-test.
- Risk of over-testing trivial code — mitigated by focusing coverage thresholds on
  `domain` and `application` rather than a blanket repo-wide number.

**Follow-ups**
- Set coverage thresholds on `domain`/`application` in CI.
- Stand up the integration-test harness (Testcontainers) for `infrastructure`.
- Reflected in `CLAUDE.md`: the work loop is red-green-refactor and DoD is test-first.
