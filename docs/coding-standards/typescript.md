# TypeScript & Angular Coding Standards

Applies to all TypeScript in Roomy — the Angular app and Nx tooling. Prettier formats;
ESLint with Angular ESLint and the Nx boundary rule enforces correctness (rules marked ⚙
are tool-enforced). Frontend feature libraries mirror the backend bounded contexts.

## TypeScript config ⚙

- `strict: true` with all strict flags, plus `noUncheckedIndexedAccess`,
  `noImplicitOverride`, `noFallthroughCasesInSwitch`, and `exactOptionalPropertyTypes`.
- No `any` — use `unknown` and narrow. Any `any` needs an inline justification.
- No non-null assertion (`!`) to bypass null checks.
- Imports use Nx path aliases; no deep relative imports across libraries.

## Naming

- Classes, interfaces, types, enums: `PascalCase`. No `I`/`T` prefixes (TS convention).
- Variables, functions, methods: `camelCase`. True constants: `CONSTANT_CASE`.
- Files: `kebab-case` with Angular role suffixes — `*.component.ts`, `*.service.ts`,
  `*.store.ts`, `*.guard.ts`.
- Booleans read as predicates: `is…`, `has…`, `can…`.
- Signals are named for the value they hold (`count`, not `countSignal`).
- Observables carry a `$` suffix (`users$`) — only where RxJS is actually used.
- Names reveal intent; this takes priority over comments.
- No single-letter or shortcut names, **including lambda / callback / array-method
  parameters** (`storedEvent`, not `e` / `x` / `s`).

## Types

- Let inference handle locals; annotate exported/public function signatures explicitly.
- `interface` for object shapes; `type` for unions, intersections, and mapped/utility
  types.
- Prefer `readonly` and `ReadonlyArray<T>`; model immutable data as immutable.
- Discriminated unions over loose optional fields. Avoid `enum` — use `as const` union
  literals.
- No type assertions (`as`) except at trust boundaries (e.g. parsed JSON), and only with
  validation.

## Domain types (no primitive obsession)

Mirror the backend: domain concepts are types, not raw `string`/`number`. Passing a bare
`string` where a `CustomerId` or `Email` is expected is a bug the type system should
catch.

- **Branded (opaque) types are the default.** TypeScript is structurally typed, so
  `type CustomerId = string` gives no protection. Use the `Brand<T, B>` helper
  (`libs/shared/util/src/branding.ts`) — e.g. `type CustomerId = Brand<string, 'CustomerId'>`,
  `type Email = Brand<string, 'Email'>`. Brands are erased at runtime (zero cost) and
  cannot be interchanged at compile time.
- **Mint branded values through a validating smart constructor**, never a bare `as` cast
  scattered around. The smart constructor is the one sanctioned place for the `as`.
  Validation lives at the trust boundary (where API data enters); inside the app the
  branded type is trusted.
- **IDs especially:** every aggregate/entity ID is its own branded type, so a `DeskId`
  can never be passed where a `UserId` is wanted.
- Reach for a **value-object class** only when the concept carries real behaviour;
  otherwise a branded type plus standalone functions is lighter and friendlier to
  signals and serialization.
- Raw `string`/`number` are reserved for genuinely primitive data and the edges (DTO/wire
  types before validation).
- **Validation policy (ADR-0020):** backend DTOs are trusted via the OpenAPI-generated
  client — no runtime re-validation; branded values are minted by mapping the generated
  DTOs at the data-access boundary. Runtime validation (and brand minting via smart
  constructors) applies only to genuinely untrusted input: form input, third-party
  APIs/webhooks, and values from URLs or storage.

## Comments & documentation

- Same policy as C#: only when needed, *why* not *what*, rename rather than comment.
- JSDoc only on the exported public API of shared libraries; no ceremonial JSDoc that
  merely echoes a name. Default to no comment.
- No commented-out code; `// TODO:` references a tracking issue.

## Functions & modules

- Small, focused, pure where possible; early returns. A single-statement guard may be one
  line (`if (!user) return;` / `if (invalid) throw new Error(...)`); braces for multi-line bodies.
- No boolean behaviour-switch parameter — use an options object or separate functions.
- Named exports only; no default exports.
- One public type per file (one Angular component / directive / service / store per file),
  the file named for it with its role suffix. Closely-related generic overloads of the same
  concept may share a file (mirrors the C# rule).
- Each library exposes a single public entry (`index.ts`); no deep imports into another
  library's internals. ⚙

## Immutability & null

- `const` by default; never reassign parameters; never mutate inputs in place.
- Optional chaining (`?.`) and nullish coalescing (`??`); avoid `!`.
- Be deliberate about returning `[]` vs `undefined`, and make which one explicit.

## Angular

- **Components:** standalone only (no `NgModule`); `ChangeDetectionStrategy.OnPush`;
  zoneless change detection.
- **State:** signals — `signal`, `computed`. Use `effect` sparingly and never to derive
  state (that is what `computed` is for). No manual `markForCheck`.
- **Inputs/outputs:** signal-based `input()`, `output()`, `model()`. No `@Input()` /
  `@Output()` decorators in new code.
- **Host bindings:** declare host bindings and listeners in the `host` metadata object of
  the `@Component` / `@Directive`; no `@HostBinding` / `@HostListener` decorators.
- **DI:** `inject()` over constructor parameters; singleton services use
  `providedIn: 'root'`.
- **Templates:** built-in control flow `@if` / `@for` / `@switch`; every `@for` uses
  `track`; no logic in templates; prefer signals/async pipe over manual subscription.
  Bind with native `[class.x]` / `[style.x]`, never `ngClass` / `ngStyle`. Prefer inline
  templates for small components; an external template/style is referenced by a path
  relative to the component `.ts` file.
- **Forms:** Reactive forms over template-driven.
- **RxJS:** only where streams are genuinely warranted; bridge with `toSignal` /
  `toObservable`; use `takeUntilDestroyed`; never nest `subscribe`; no manual
  `subscribe` in a component when a signal or the async pipe will do.
- **Smart/dumb split:** presentational components are pure (inputs in, outputs out, no
  injected services); container components wire data.
- **HTTP:** typed clients in services; components never call `HttpClient` directly.
- **Feature libs mirror the backend bounded contexts**; Nx tags enforce the boundaries. ⚙
- **State beyond component scope:** NgRx SignalStore (`@ngrx/signals`) — signal-native,
  low boilerplate (ADR-0019). Component-local state stays plain signals.
- **Styling:** vanilla CSS as long as it suffices — design tokens as CSS custom
  properties, native nesting, component-scoped, no global leakage. Adopt SCSS/SASS or
  Tailwind only when a concrete need arises (ADR-0019).
- **Images:** `NgOptimizedImage` for static images (it does not apply to inline base64).
- **Components:** built on Angular CDK primitives for behaviour/accessibility, styled
  with own CSS; no styled component library adopted wholesale (ADR-0021).
- **Localization:** all user-facing text via Transloco (DE + EN), no hardcoded strings,
  runtime language switching; locale-aware date/number formatting via `Intl` (ADR-0024).
- **Accessibility:** target WCAG 2.2 AA — semantic HTML, labelled controls, keyboard
  support, focus management via the CDK, sufficient contrast (ADR-0024).

## Project structure (Nx) ⚙

- **Import scope `@roomy/*`.** Libraries are imported by alias — e.g.
  `@roomy/attendance-feature`, `@roomy/shared-ui` — wired in `tsconfig.base.json`.
  Libraries are internal (`private`), not published to a registry.
- **Library naming `@roomy/<context>-<type>`**, folders `libs/<context>/<type>`, where
  `type` is the Angular library kind: `feature | ui | data-access | util` (e.g.
  `libs/attendance/feature` → `@roomy/attendance-feature`). Cross-cutting libraries use
  the `shared` context: `@roomy/shared-ui`, `@roomy/shared-util`.
- **Tag every project** with `context:<name>` and `type:…`; `@nx/enforce-module-boundaries`
  defines the allowed dependencies (e.g. `feature` may use `ui`/`data-access`/`util`;
  `util` depends on nothing), so a forbidden cross-import fails lint. Backend libraries
  carry equivalent layer tags.
- `nx affected` scopes lint, test, and build to what changed.

## Testing

- TDD (ADR-0009).
- **Runner:** Vitest via Nx (ADR-0019).
- Component tests with Angular Testing Library — assert behaviour through the DOM, not
  implementation details.
- Test names state behaviour; Arrange–Act–Assert; no logic in tests; builders for
  fixtures.

## Formatting ⚙ (Prettier is the source of truth)

- 2-space indentation, single quotes, semicolons, trailing commas, ~100–120 columns.
- Import ordering via ESLint; no unused imports.
- `prettier --check` and `eslint` run in CI.
