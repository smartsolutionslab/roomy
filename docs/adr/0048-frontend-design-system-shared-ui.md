# 0048. Frontend design system in shared-ui: global tokens/base + primitive directives and components

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

The rebrand (ADR-0047) gave Roomy a coherent visual language, but its building blocks were
scattered. The two core surfaces — the button (`.btn`) and the glass card (`.card`) — lived
in the app's component CSS (`apps/web/src/app/app.css`, `home.css`), so Angular view
encapsulation kept the feature libraries from reusing them. Every feature page therefore
re-implemented the same patterns in its own stylesheet: a page container (flex column + gap +
max-width), label/input fields, lists, error text, status lines, tables, and `dl` metadata —
and they had drifted (hardcoded `1rem`/`0.25rem` instead of tokens, `outline: 2px solid
currentcolor` instead of the focus token, max-widths of 40/44/48rem). The design tokens were
global and good, but owned by the app rather than by a design system.

How should the design system be packaged so every page composes the same primitives, with one
source of truth for tokens, spacing, and accessibility?

## Decision drivers

- **Reuse across libraries.** Feature libs (`type:feature`) must be able to use the same
  button/card/field as the app — view-encapsulated component CSS in the app cannot be shared.
- **One source of truth.** Tokens, focus ring, and the primitive look should live in one place.
- **Idiom + boundaries (ADR-0019/0021/0035).** Stay vanilla-CSS and standalone/signal; keep the
  primitives in `@roomy/shared-ui` (`type:ui`/`context:shared`) so `feature → ui` consumes them.
- **Low churn.** Migrating pages should mostly be swapping classes/wrappers, not rewrites.

## Considered options

- **A — Pure component library.** A component for every primitive (`<roomy-button>`, …).
  Idiomatic but heavy: wrapping native `<button>`/`<input>` in components complicates forms,
  submit, and `formControlName`, and is more churn.
- **B — Pure global utility CSS.** One shared stylesheet of classes the pages apply. Lowest
  churn, but abandons the component idiom entirely for interactive elements and offers no typed
  API.
- **C — Hybrid (chosen).** A global design-system stylesheet for tokens/base/utilities, plus
  thin directives over native elements and small components where structure/a11y warrant.

## Decision

**Option C.** `@roomy/shared-ui` becomes the design-system home:

1. **`libs/shared/ui/src/styles/design-system.css`** owns the `@font-face`, the token `:root`/
   dark blocks, base element styling, a **normalized focus ring** (`:where(...):focus-visible`),
   and the global utility classes (`.roomy-button`, `.roomy-card`, `.roomy-page`, `.roomy-list`,
   `.roomy-table`, `.roomy-dl`, `.roomy-visually-hidden`). It is loaded once by the app build
   (`apps/web/project.json` `styles`, before the trimmed app `styles.css`).
2. **Directives** apply the global classes to native elements (keeping semantics/forms):
   `roomyButton` (`variant`), `roomyCard` (`interactive`), `roomyPage` (`size`).
3. **Components** own structure + a11y: `roomy-form-field` (label wrapping a projected control —
   implicit association) and `roomy-message` (`variant` `error`/`status` → `role` + `aria-live`).
4. The app shell and **all** feature pages migrate onto these, deleting their duplicated CSS;
   only genuinely page-specific layout (the room picker, occupancy rollup, calendar grid, nested
   room rows) stays local.

## Consequences

**Positive**
- One source of truth for tokens, spacing, focus, and the primitive look; pages get consistent
  buttons/cards/fields/errors for free, and per-page CSS shrinks to near-nothing.
- Primitives are presentational and locale-agnostic (text is projected/passed in) and unit-tested
  in `shared-ui`.

**Negative / trade-offs**
- A deliberate **global CSS layer** now exists alongside component-local CSS — an evolution of
  ADR-0019/0021, justified for a design system. Authors must pick `accent` vs default and the
  right `roomyPage` size.
- The global utility classes bypass encapsulation by design; they are namespaced `roomy-*`.
- `roomy-form-field` projects the control into a `<label>`, which trips
  `@angular-eslint/.../label-has-associated-control` (it can't see through projection) — one
  scoped, justified disable; the association is verified by `getByLabelText` in the spec.

**Follow-ups**
- No taxonomy change: the primitives sit inside the existing `type:ui`/`context:shared` rules.
- Future primitives (e.g. a dialog, a toast) extend the same library.
