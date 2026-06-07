# C# Coding Standards

Applies to all .NET code in Roomy. The `.editorconfig` and analyzers enforce the
mechanical rules (marked ⚙); the rest is upheld in review. These standards assume and
extend the architecture decisions in ADR-0003 (Clean Architecture + DDD) and ADR-0005
(no framework in the core).

## Language & compiler ⚙

- Target .NET 10, latest C# language version.
- `Nullable` enabled solution-wide; nullable warnings are errors.
- Warnings treated as errors; .NET analyzers at the latest analysis level.
- File-scoped namespaces. One top-level type per file; file name matches the type.
- **Root namespace `SmartSolutionsLab.Roomy`** (pattern `SmartSolutionsLab.{ProjectName}`);
  per-project namespaces extend it following the folder structure, e.g.
  `SmartSolutionsLab.Roomy.SharedKernel.Guards`. Set via `<RootNamespace>` in
  `Directory.Build.props`.
- `using` directives sorted, `System.*` first; unused usings removed.
- `ImplicitUsings` enabled to cut framework noise; anything project-specific stays an
  explicit `using`.

## Naming

- Types, methods, properties, events, constants: `PascalCase`.
- Local variables and parameters: `camelCase`.
- Private fields: `camelCase`, no underscore prefix (instance and static alike).
  Disambiguate from a parameter of the same name with `this.` where needed.
- Interfaces: `I` prefix. Type parameters: `T`, or descriptive `TKey` / `TResult`.
- Async methods returning a `Task`/`ValueTask`: `Async` suffix.
- Booleans read as predicates: `Is…`, `Has…`, `Can…`, `Should…`.
- No abbreviations, no Hungarian notation. Names reveal intent — this is the primary
  documentation mechanism and takes priority over comments.
- Avoid grab-bag names (`Manager`, `Helper`, `Util`, `Service` with no object); name by
  responsibility.

## Comments & documentation

- Comments only when needed. A comment explains *why* something non-obvious is done; it
  never restates *what* the code does. If a comment is needed to explain *what*, rename
  instead.
- No commented-out code — that is what version control is for.
- XML doc comments only on public, reusable API surface (e.g. `shared-kernel`); not on
  internal domain types whose names already carry the meaning.
- `// TODO:` must reference a tracking issue.

## Domain modeling (DDD)

- No primitive obsession: model concepts as types. `Email`, `CustomerId`, `DeskNumber`
  are value objects, not `string`/`Guid`/`int`.
- Value objects are immutable with value equality — `record` or `readonly record struct`
  where appropriate — and validate in the factory/constructor with `Ensure`.
- Entities and aggregates have identity-based equality, no public setters; state changes
  go through intention-revealing methods that preserve invariants.
- The aggregate is the consistency boundary; reference other aggregates by ID only.
- Encapsulate collections: expose `IReadOnlyList<T>`/`IReadOnlyCollection<T>`, mutate
  only through methods.
- Raise domain events for in-context reactions; cross-context flows use integration
  events (ADR-0005).
- `sealed` by default; allow inheritance only deliberately.

## Null & validation

- NRT is enabled and enforced (nullable warnings are errors), and we lean on it: the
  type system — not defensive code — is the primary null guarantee. Do **not** sprinkle
  null checks across internal, NRT-checked code; a non-nullable reference is already
  guaranteed non-null by the compiler, so checking it again is dead code.
- Runtime null checks belong only at **trust boundaries**, where values enter from
  outside the NRT-checked world: deserialized payloads, external/third-party APIs,
  reflection/serialization, and public API consumed by callers that may ignore the
  annotations. NRT is compile-time only and can be violated at runtime there.
- Never use the null-forgiving `!` to silence the compiler — fix the nullability.
- **`Ensure.That(...)` is for argument checking and throws `ArgumentException`s**
  (`ArgumentNullException`, `ArgumentOutOfRangeException`, …). Use it for preconditions
  the type system cannot express — a string that must be non-empty, a value in range, a
  trust-boundary value that must be non-null — not to re-check non-nullable references
  internally.
- Express "may be absent" with `T?`, not with `null` smuggled through a non-nullable
  type. Never return `null` for a collection — return empty.
- Invariant violations and programmer errors throw (via `Ensure`); infrastructure faults
  throw. **Expected business outcomes** (e.g. "desk already booked") return a
  `Result`/`Result<T>` from `SmartSolutionsLab.Roomy.SharedKernel.Results` — explicit, never an exception.
  Application use cases return `Result`; the API layer maps `Error.Type` to an HTTP
  status.

## Methods & control flow

- Small, single-purpose methods. If you need a comment to delimit a section of a method,
  extract a method instead.
- Guard clauses and early returns over nested `if`.
- No boolean parameter that switches behaviour — use an enum or two methods.
- Introduce a parameter object beyond ~3 related parameters.
- Expression-bodied members when they read clearly; pattern matching / switch
  expressions over long `if`/`else` chains.

## Async

- Async all the way. Never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Flow a `CancellationToken` through all I/O-bound and long-running paths.
- No `async void` except event handlers.
- `ValueTask` only with a measured reason. Don't wrap synchronous work in `Task.Run` in
  server code.

## Error handling

- Exceptions are for the exceptional; not for normal control flow.
- Expected business failures use `Result` (see Null & validation), not exceptions.
- Catch only what you can handle; never swallow (no empty `catch`).
- Use domain-specific exception types where it aids handling; don't catch `Exception`
  broadly. Don't log-and-rethrow at every layer.

## Dependencies (Clean Architecture) ⚙ via architecture tests

- `domain` depends on nothing; `application` only on `domain`; `infrastructure` depends
  inward; hosts compose (ADR-0003).
- No framework types in `domain`/`application` — no Wolverine, no MediatR, no EF Core
  types leaking through. Use owned abstractions/ports (ADR-0005).
- Constructor injection only; no service locator; no static mutable state.

## Repositories & persistence

- One repository per aggregate root, with collection-style semantics: `Add`, `Remove`,
  and intent-named queries (`FindByEmail`). Persistence is an implementation detail
  behind an `application` port.
- Never leak `IQueryable` out of `infrastructure`.

## Testing

- TDD: tests precede implementation (ADR-0009).
- Test names state behaviour (`Method_state_expectedResult` or a sentence).
- Arrange–Act–Assert; one logical assertion per test.
- No control flow or logic in tests; use builders for test data.
- `domain`/`application` tests are pure and fast; `infrastructure` is covered by
  integration tests (Testcontainers).

## Formatting ⚙ (`.editorconfig` is the source of truth)

- 4-space indentation; Allman braces; braces always, even for single statements.
- ~120-column soft limit.
- `var` when the type is obvious from the right-hand side; explicit type otherwise.
- Trailing commas in multiline initializers.
- `dotnet format --verify-no-changes` runs in CI.
