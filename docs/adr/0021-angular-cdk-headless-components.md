# 0021. UI on Angular CDK + headless components, styled with own CSS

- **Status:** Accepted
- **Date:** 2026-06-07
- **Deciders:** Heiko Weiß

## Context and problem statement

The frontend needs accessible, robust UI behaviours (overlays, focus management, a11y,
drag-and-drop) without an imposed styling/theming layer that would conflict with the
vanilla-CSS-first direction (ADR-0019) and the preference for control.

## Decision drivers

- Keep styling control (own vanilla CSS); no imposed theme to fight.
- Get accessibility-critical behaviours without reinventing them.
- Minimal imposed dependencies (framework-independence instinct).

## Considered options

- **Angular CDK + headless components, styled with own CSS.**
- PrimeNG — large ready-styled set; velocity, but its own theming layer.
- Angular Material — comprehensive and accessible, but strongly Material-flavoured.

## Decision

Build the UI on **Angular CDK** primitives for behaviour and accessibility, and style
components with **own vanilla CSS**. Presentational components in the shared UI library
wrap CDK primitives; no styled component library is adopted wholesale.

## Consequences

**Positive**
- Full styling control, consistent with the vanilla-CSS direction.
- Accessibility-critical behaviours come from the CDK rather than being hand-rolled.
- Minimal imposed dependencies and no theming layer to override.

**Negative / trade-offs**
- Rich widgets (calendars, date pickers, data tables) are tedious to build headless —
  exactly the area a styled library would accelerate.
- Mitigation: if a specific heavy widget proves too costly to hand-build, adopt a focused
  library for that one piece rather than abandoning the headless approach.

**Follow-ups**
- Build the shared UI library on CDK primitives.
- Reassess a targeted library only for a specific complex widget if it becomes a
  bottleneck.
