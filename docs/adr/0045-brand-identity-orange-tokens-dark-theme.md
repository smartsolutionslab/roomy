# 0045. Brand identity: burnt-orange two-accent token system, dark theme, and monogram logo

- **Status:** Accepted
- **Date:** 2026-06-10
- **Deciders:** Heiko Weiß

## Context and problem statement

The web app (`apps/web`) had no product identity: a generic blue accent (`#1d4ed8`),
text-only branding in the header, the default Angular `favicon.ico`, and a thin token set
(one accent, one radius, no elevation/transition/dark scale). Several feature views had
colours hard-coded in component CSS (`#b00020`, `#ccc`, `#f4f4f4`) rather than reading
tokens, so a theme change could not reach them.

We want a modern, recognizable Roomy identity built around **orange** as the primary
colour, including a custom logo/favicon, and (a product decision taken with this work) a
**dark theme**. The question is how to express that identity as a maintainable, accessible
design system within our existing constraints — vanilla CSS (ADR-0019/0021), WCAG 2.2 AA
and Transloco DE+EN (ADR-0024), and the frontend library boundaries (ADR-0035).

## Decision drivers

- **Accessibility (ADR-0024).** WCAG 2.2 AA contrast is a hard gate. Burnt orange `#EA580C`
  with white text is only ~3.5:1 — fine as a brand/decorative tone, but it fails AA for
  text. The system must not let a single "accent" token be used where it breaks contrast.
- **Single source of truth.** Recolouring or theming must happen in one place and reach
  every view, including the feature libs — no colour literals stranded in component CSS.
- **No new stack (ADR-0019/0021).** Stay on vanilla CSS custom properties; no Tailwind,
  no CSS-in-JS, no component library.
- **Boundaries (ADR-0035).** A `type:ui` lib may depend only on `ui`/`util`; anything that
  needs a service (the theme state) must live in `type:feature`.
- **Theme without a flash, SSR/zoneless-safe.** First paint should respect the OS
  preference; an explicit choice must persist and win.

## Considered options

- **A — Single `--accent` token recoloured to orange.** Simplest, but forces the one
  orange tone into both decorative and text-bearing roles, guaranteeing a contrast
  failure somewhere. Rejected on accessibility.
- **B — Adopt a CSS framework / design-token library (Tailwind, Material) for the palette
  and dark mode.** Off-the-shelf theming, but contradicts ADR-0019/0021 and is a large,
  cross-cutting dependency for a styling refresh. Rejected.
- **C — Two-accent token system in the existing vanilla-CSS variables, with a dark theme
  expressed as token overrides and a small theme service (chosen).** Fits the stack and
  boundaries, keeps one source of truth, and encodes the contrast rule in the tokens
  themselves.

## Decision

We chose **Option C**.

1. **Two-accent token model** in `apps/web/src/styles.css`. `--roomy-color-accent`
   (`#EA580C`) is the **brand/decorative** tone (logo, borders, gradient, large/icon use);
   `--roomy-color-accent-strong` (`#C2410C`, ~5:1 on white) is the **text-bearing** tone
   for buttons/links and the focus ring. Supporting `*-hover/-subtle/-muted/-on-accent`
   tokens complete the ramp, alongside a refreshed cool-zinc neutral palette and new
   `radius`, `shadow`, `transition`, type-scale, and `gradient-accent` tokens. Existing
   token *names* are preserved so already-merged component CSS keeps working.
2. **Dark theme as token overrides** under both `@media (prefers-color-scheme: dark)`
   (scoped to `:root:not([data-theme="light"])`) and `:root[data-theme="dark"]` (explicit
   choice wins). `html { color-scheme: light dark }` so native controls follow the theme.
   A signal-based `ThemeService` (`@roomy/shared-data-access`) resolves the initial theme
   from `localStorage` then OS preference, reflects it onto a `data-theme` attribute on
   `<html>`, and persists explicit toggles; `localStorage`/`matchMedia` access is guarded
   for zoneless/SSR. The `ThemeToggle` control lives in `@roomy/shared-feature` (it depends
   on the service — a `feature`, not a `ui`, per ADR-0035) and is localized + accessible.
3. **Brand mark.** A hand-authored geometric "R" monogram on a gradient burnt-orange
   rounded tile, shipped as `icon.svg`/`logo.svg` plus generated `favicon.ico` (16/32/48)
   and `apple-touch-icon.png`. A theme-aware `RoomyLogo` (`@roomy/shared-ui`, pure
   presentational) renders the inline mark + a `currentColor` wordmark.
4. **Tokenize stranded literals.** The hard-coded colours in attendance feature CSS are
   replaced with `--roomy-color-danger` / `-border` / `-background-muted` so dark theme is
   correct everywhere.

## Consequences

**Positive**
- One source of truth for colour and theme; recolouring or adjusting the dark palette is a
  token edit. The contrast rule is encoded in the tokens (decorative vs text-bearing), not
  left to each author to remember.
- Dark mode and a recognizable logo/favicon with no new runtime dependency; stays within
  ADR-0019/0021 and the ADR-0035 boundaries.

**Negative / trade-offs**
- Two accent tokens are a small ongoing discipline: authors must pick `accent` vs
  `accent-strong` correctly. Documented by the token comments and this ADR.
- jsdom has no `matchMedia`; the guard makes the unnecessary-looking optional-chaining
  deliberate (kept because the project's ESLint does not enable `no-unnecessary-condition`).

**Follow-ups**
- Responsive breakpoints / mobile nav and any further component polish are out of scope and
  can follow as separate slices.
- No taxonomy change to `CLAUDE.md`: `ThemeService` (`data-access`), `ThemeToggle`
  (`feature`), and `RoomyLogo` (`ui`) all sit inside existing `context:shared` rules.
