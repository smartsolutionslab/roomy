# 0024. Frontend baselines: localization (Transloco, DE + EN) and WCAG 2.2 AA accessibility

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

Roomy is a German B2B product, so it needs localization (at least DE, likely EN), and
B2B procurement commonly requires an accessibility standard. Both are cross-cutting and
expensive to retrofit into a built UI, so they are pinned up front even though most
features come later.

## Decision drivers

- DE and EN support, with runtime language switching (not build-per-locale).
- Retrofitting i18n into an existing UI is painful — design it in.
- A defensible accessibility target for B2B.

## Decision

- **Localization via Transloco**, languages **DE + EN**. All user-facing text is
  externalized — no hardcoded strings — with runtime language switching. Locale-aware
  date/number formatting via `Intl`. (Transloco's runtime model is preferred over
  Angular's build-per-locale i18n for live switching.)
- **Accessibility target WCAG 2.2 AA:** semantic HTML, labelled controls, full keyboard
  support, focus management via the Angular CDK (ADR-0021), and sufficient contrast.
  Automated a11y checks (e.g. axe) in component/e2e tests where feasible.

## Consequences

**Positive**
- Localization is designed in — adding languages later is cheap.
- A clear accessibility baseline, which also eases B2B procurement.

**Negative / trade-offs**
- Discipline cost: every user-facing string goes through Transloco; every component is
  reviewed for a11y.

**Follow-ups**
- Transloco setup with DE/EN message files; a lint/review rule against hardcoded
  user-facing strings.
- Automated a11y checks (axe) in component and e2e tests.
- Locale-aware formatting via `Intl`.
