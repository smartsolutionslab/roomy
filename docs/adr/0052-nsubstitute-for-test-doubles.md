# 0052. NSubstitute for test doubles in unit tests

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

The unit suites (`backend/tests/attendance`, `backend/tests/identity`, `backend/tests/organization`) drive application
handlers against the ports they depend on (`IAttendanceDayRepository`, `IUserRepository`,
`IIdentityProviderPort`, `IUnitOfWork`, the read-model ports, …). Until now every such double was
hand-rolled: a `private sealed class FakeRepository : IPort` per test file, implementing the whole
interface even when a test exercised one method, counting calls in fields (`SaveCount`, `LoadCount`),
queueing canned results, and frequently embedding assertions inside the double
(`employee.ShouldBe(expected)`).

This costs more than it gives: each new port method forces edits to every hand-rolled double that
implements it; the call-counting and result-queueing boilerplate is re-implemented per file; and
assertions hidden inside a double are easy to miss when reading a test. The doubles are pure
interaction/stub plumbing — exactly what a substitution library exists to remove.

Which mocking/substitution library, if any, should the unit tests standardise on?

## Decision drivers

- **Less boilerplate.** A double should declare only the behaviour a test needs, not the whole
  interface.
- **Interaction verification belongs in the Assert.** Call-count and argument checks should read as
  assertions in the test body, not as side effects buried in a fake.
- **Licensing.** Must be free/OSS for commercial use — the same constraint that ruled out
  FluentAssertions v8 in favour of Shouldly (see `docs/testing-strategy.md`).
- **Idiomatic, well-known API.** Low learning cost; good xUnit v3 / async support.
- **Keep genuine state where state is the point.** Some doubles model behaviour (an aggregate replayed
  from history); the choice must not force that into awkward lambdas.

## Considered options

- **A — Keep hand-rolling all doubles.** No dependency; maximal boilerplate and the per-method-edit tax.
- **B — Moq.** Capable and popular, but the 4.20 `SponsorLink` telemetry episode makes it a
  governance risk for a B2B product.
- **C — NSubstitute (chosen).** MIT-licensed, no telemetry, concise non-mocky API
  (`Substitute.For<T>()`, `.Returns(...)`, `.Received(n)`, `Arg.*`), first-class async.
- **D — FakeItEasy.** Comparable and acceptable; NSubstitute chosen for its terser syntax and larger
  install base in the .NET community.

## Decision

**Option C.** Adopt **NSubstitute** as the standard test-double library for the .NET **unit** suites.

- Added to Central Package Management (`Directory.Packages.props`, ADR-0043) and referenced by the unit
  test projects (`backend/tests/attendance`, `backend/tests/identity`, `backend/tests/organization`).
- **Stubs** return canned values with `port.Method(Arg.Any<…>()).Returns(Result.Success(value))`;
  **interaction** is verified in the Assert with `await port.Received(1).Method(expectedArg, …)` /
  `DidNotReceive()`; arguments are captured with `Arg.Do<T>(list.Add)` and asserted with the existing
  Shouldly helpers (`ShouldHaveSingleItem()`, …).
- **Assertions never live inside a double.** What a hand-rolled fake checked inline becomes a
  `Received(…)` call or a captured-argument assertion.
- **State where state is the point stays explicit.** A handler under an optimistic-retry loop is driven
  with NSubstitute's consecutive returns (`.Returns(first, second)` / per-call factory lambdas for a
  fresh aggregate each load); this fully replaces the queue-based hand-rolled repositories.
- **Scope: unit suites only.** Integration/e2e tests run against the real Aspire-composed stack
  (Postgres, Keycloak, RabbitMQ) and do not mock; ASP.NET test infrastructure such as `TestAuthHandler`
  is a real handler, not a double, and is unchanged.
- **Assertions remain Shouldly.** NSubstitute supplies the doubles; Shouldly the assertions
  (`docs/testing-strategy.md`). The NSubstitute analyzers package is not added for now to avoid
  interaction with `TreatWarningsAsErrors`; it can be revisited.

## Consequences

**Positive**
- Doubles declare only what a test uses; adding a port method no longer ripples through unrelated fakes.
- Interaction checks read as assertions; nothing hides in a fake.
- Less per-file plumbing; tests are shorter and more uniform.

**Negative / trade-offs**
- A new test dependency and a small API to learn.
- Runtime (dynamic-proxy) substitution: a typo'd setup fails at run time, not compile time — mitigated
  by the green-bar discipline and optionally the analyzers later.
- One-off churn converting the existing hand-rolled doubles in the unit suites.

**Follow-ups**
- `docs/testing-strategy.md` and `CLAUDE.md` record the convention; new unit tests use NSubstitute for
  doubles.
