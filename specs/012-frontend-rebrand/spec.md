# Feature Specification: Frontend rebrand — vivid-orange identity, logo, dark theme, sidebar shell

**Feature Branch:** `feat/012-frontend-rebrand`
**Status:** Draft
**Created:** 2026-06-10
**Updated:** 2026-06-10
**Realizes:** product request for a modern Roomy visual identity built around orange, a
custom logo/favicon, and a dark theme. Design-system decision recorded in **ADR-0045**
(within the vanilla-CSS, WCAG 2.2 AA + Transloco, and frontend-boundary constraints of
ADR-0019/0021/0024/0035).

## Summary

The web app had no product identity: a generic blue accent, text-only header branding, the
default Angular favicon, a thin token set, and colours hard-coded in some feature CSS. This
feature gives Roomy a recognizable, modern look: a **vivid-orange** brand palette with
**sunset (orange→amber→rose) gradients**, expressed as a centralized **two-accent token
system** (a decorative tone plus an AA-safe text tone); a **custom monogram logo + favicon**;
a refreshed neutral/elevation/type scale plus **glow and glass** tokens; a **dark theme**
that follows the OS preference and can be toggled; and a **richer layout** — a **left
sidebar** for signed-in users, a **gradient hero landing** for signed-out visitors, and a
**dashboard** of navigation cards on the signed-in home. All visible strings stay localized
(DE + EN) and the shell remains WCAG 2.2 AA.

## User Scenarios & Testing

### Primary User Story

As a user opening Roomy, I want a clear, modern, recognizable product identity (logo,
brand colour, tab icon) and the option of a light or dark appearance, so the app feels
purpose-built and comfortable to use in any lighting.

### Acceptance Scenarios

1. **Brand colour applied**
   - GIVEN any page of the app
   - WHEN it renders
   - THEN interactive accents (buttons, links, focus ring, active nav) use the burnt-orange
     brand, not the previous blue.

2. **Logo and favicon**
   - GIVEN the app shell
   - THEN the header shows the Roomy monogram logo with the wordmark, and the browser tab
     shows the Roomy icon (not the default Angular favicon).

3. **Theme follows the OS on first visit**
   - GIVEN a user who has never chosen a theme
   - WHEN the OS prefers dark
   - THEN the app renders in dark theme (and light when the OS prefers light).

4. **Theme can be toggled and persists**
   - GIVEN the theme toggle in the header
   - WHEN the user switches theme and later reloads
   - THEN the chosen theme is reapplied (the explicit choice wins over the OS preference).

5. **Contrast holds in both themes (ADR-0024)**
   - GIVEN light or dark theme
   - THEN body text on its background, and text on the strong-accent control, meet WCAG 2.2
     AA (≥ 4.5:1); the brand/decorative accent is never used as a background behind small
     text.

6. **Theme control is accessible and localized**
   - GIVEN a keyboard/screen-reader user
   - THEN the toggle is operable, exposes a name describing the action it performs, conveys
     the active state, and its text is available in DE and EN.

7. **The whole UI themes (no stranded colours)**
   - GIVEN dark theme
   - THEN feature views (error messages, the occupancy calendar) follow the theme — no
     element keeps a hard-coded light-only colour.

8. **Shell stays accessible**
   - GIVEN the redesigned shell
   - THEN it has no detectable accessibility violations (automated axe check) and the
     skip-link / keyboard navigation still work.

9. **Hero landing for signed-out visitors**
   - GIVEN a visitor with no session
   - THEN the home page shows a gradient hero (brand mark, headline, subtitle) and a clear
     Sign-in call to action, over a glass top bar — not a bare welcome line.

10. **Sidebar + dashboard for signed-in users**
   - GIVEN a signed-in user
   - THEN navigation is an orange (inverted) left sidebar with white brand + icon links,
     while the current user, theme toggle, language switcher, and sign-out sit in a glass
     top app bar at the right edge; the home page is a dashboard of cards linking to the
     user's available sections (admin-only cards only for administrators).

### Edge Cases
- `localStorage` unavailable (private mode / SSR) → theme still resolves and applies; the
  choice simply is not remembered.
- `matchMedia` absent (e.g. test/SSR environment) → defaults to light without error.
- No-JS / first paint before the theme service runs → the `prefers-color-scheme` media
  rule governs, so the initial colours are still correct.

## Requirements

### Functional Requirements
- **FR-001:** A single global token set MUST define the palette; recolouring or theming
  happens there and reaches every view (ADR-0045). No colour literals in component CSS.
- **FR-002:** The accent MUST be modelled as two tokens — a brand/decorative tone and an
  AA-safe text-bearing tone — and text-on-accent MUST use the AA-safe tone (≥ 4.5:1).
- **FR-003:** The app MUST ship a custom Roomy logo (header) and favicon/app icons
  (`icon.svg`, `favicon.ico`, `apple-touch-icon.png`); the logo MUST be theme-aware.
- **FR-004:** A dark theme MUST exist, default to the OS `prefers-color-scheme` on first
  visit, be switchable via a header control, and persist an explicit choice (which wins).
- **FR-005:** The theme control MUST meet WCAG 2.2 AA (operable, named by action, state
  conveyed) and be localized DE + EN (ADR-0024).
- **FR-006:** Theme state MUST be owned by a service in `@roomy/shared-data-access` and the
  toggle by a component in `@roomy/shared-feature`; the logo is a presentational component
  in `@roomy/shared-ui` (ADR-0035 boundaries). Service access to browser globals MUST be
  guarded for zoneless/SSR.
- **FR-007:** The redesigned shell MUST keep the existing accessibility guarantees (axe:
  no violations; skip-link and keyboard nav intact).
- **FR-008:** The shell MUST present a left sidebar for signed-in users and a glass top bar
  + gradient hero landing for signed-out visitors; the signed-in home MUST be a dashboard of
  navigation cards. Admin-only destinations MUST appear only for administrators (unchanged
  authorization — ADR-0040 guards still gate the routes themselves).

### Key Entities (view models)
- **ThemePreference** — `'light' | 'dark'`; the resolved/persisted appearance choice.

## Out of Scope (this feature / deferred)
- Polished mobile navigation (the sidebar collapses at narrow widths, but a full mobile
  drawer/responsive pass is deferred) and marketing pages beyond the signed-out hero.
- Restyling every feature page's internals (they inherit the new tokens; per-page card
  layouts can follow).
- Adopting a CSS framework or component library (explicitly rejected — ADR-0019/0021/0045).
- Per-context theme variations; animation beyond simple hover/transition.

## Review & Acceptance Checklist
- [ ] Burnt-orange accent applied app-wide; no residual blue
- [ ] Two-accent model used correctly (decorative vs AA-safe text tone)
- [ ] Custom logo in header + custom favicon/app icons
- [ ] Dark theme: OS-default on first visit, toggle, persistence (explicit wins)
- [ ] AA contrast verified in light and dark
- [ ] Theme toggle accessible + localized DE/EN; no hard-coded strings
- [ ] No stranded colour literals in feature CSS
- [ ] Shell axe check green; skip-link/keyboard nav intact
- [ ] `pnpm nx affected -t lint test build` green
