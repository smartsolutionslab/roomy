# 0019. Frontend tooling: NgRx SignalStore, vanilla CSS (deferring preprocessors), Vitest

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

With a single signals-first, zoneless Angular app (ADR-0016), we need to choose
cross-component state management, the styling approach, and the test runner. The
preference is for signal-native, low-ceremony tools, and to defer any commitment that can
be deferred.

## Decision

- **Cross-component state — NgRx SignalStore (`@ngrx/signals`).** Signal-native and
  low-boilerplate, consistent with the zoneless/signals direction. Component-local state
  stays plain signals; SignalStore is for state shared across a feature.
- **Styling — vanilla CSS as long as it suffices.** Modern CSS only: design tokens as CSS
  custom properties, native nesting, `@layer`, component-scoped styles, no global leakage.
  **Defer** SCSS/SASS or Tailwind until a concrete need arises. Keeping tokens in custom
  properties and styles scoped makes a later switch additive rather than a rewrite.
- **Test runner — Vitest via Nx.** Fast and ESM-native, with good Nx integration; Angular
  Testing Library for component tests (per the TS standards).

## Consequences

**Positive**
- Signal-native state with minimal boilerplate.
- Styling stays dependency-free and standards-based; the door to a preprocessor/Tailwind
  stays open at low cost via custom-property tokens.
- Vitest is fast and integrates cleanly with the Nx workspace.

**Negative / trade-offs**
- NgRx SignalStore is a dependency and a pattern to learn (mitigated by prior NgRx
  experience).
- Vanilla CSS lacks some preprocessor conveniences; modern CSS covers most — revisit if
  styling complexity grows.
- Vitest with Angular is less battle-worn than Jest in some setups (improving) — accepted.

**Follow-ups**
- NgRx SignalStore per feature where cross-component state is needed.
- Establish the CSS custom-property design tokens.
- Configure Vitest in the Nx workspace.
- Component-library choice (Material / PrimeNG / a headless kit) is a separate follow-up.
