# 0035. Frontend feature-library structure and Nx tags

- **Status:** Accepted
- **Date:** 2026-06-09
- **Deciders:** Heiko Weiß

## Context and problem statement

ADR-0016 chose "a single Angular app + **feature libraries per context**" but did not pin down
the library structure, the import-path convention, or — crucially — how the Nx module-boundary
tags (ADR-0002/0003) apply to frontend libraries. Until now the SPA kept everything inline under
`apps/web/src/app`, with only one real library (`@roomy/util`, `type:util`/`context:shared`).

Building the first context-specific UI (identity: account page, admin screens, route guards —
`005-identity-web`) forces the question. Two gaps in the existing taxonomy block it:

1. The `type:*` axis is `domain · application · infrastructure · app · util` — all **backend**
   layers. There is no library type for an Angular **feature** (smart, routed UI) or its
   **data-access** (typed gateway clients). A frontend lib has nowhere to sit.
2. Context isolation says a `context:*` project may depend only on its own context and
   `context:shared`. But ADR-0016/0030 mandate **one** Angular app spanning **all** contexts
   (unlike the backend, where each context has its own host). The SPA must therefore import
   `context:identity`, `context:organization`, … feature libs — which the current rule forbids
   for its `context:shared` tag.

## Decision

**1 — Frontend feature libraries per context, mirroring the backend folder/alias convention.**
Angular libraries live at `libs/<context>/<type>` with the import alias `@roomy/<context>-<type>`
(ADR-0016). This slice introduces `libs/identity/feature` → `@roomy/identity-feature`.

**2 — Extend the `type:*` axis with frontend library types:** `feature` (smart, routed UI for a
context), `data-access` (typed gateway clients, view models, client-side state), and `ui`
(presentational, reusable). They are added as needed; `005` introduces only `feature` (its few
gateway clients live in the feature lib for now and split into a `data-access` lib if they grow).
The frontend dependency rule, encoded in `eslint.config.mjs`:
`feature → feature, ui, data-access, util`; `ui → ui, util`; `data-access → data-access, util`.
The composition-root app (`type:app`) may additionally depend on `feature`/`ui`/`data-access`.

**3 — Introduce `context:web` for the single SPA.** The one Angular app is the *frontend
composition root*; it is retagged from `context:shared` to `context:web` and may depend on any
context's frontend libs plus `context:shared`. This is the **only** project tagged `context:web`.
Backend context isolation is unchanged — the exception exists solely because ADR-0016/0030 mandate
a single app across contexts, whereas backend contexts each deploy their own host. Context feature
libs keep their own `context:<context>` tag, so `identity-feature` still **cannot** import
`organization-feature` — only the app composes across contexts.

**4 — Libraries are tested with vitest (`vitest-analog`) + `@testing-library/angular`.** The app
uses Angular's `@angular/build:unit-test`, which is application-only (it needs a build target). For
libraries we use the Nx-supported `vitest-analog` runner — same vitest engine, same
`@testing-library/angular` test code, no app build required. Test *code* is identical across app
and libs; only the runner wiring differs.

## Considered options

- **A — Keep building inline under `apps/web` (deferring ADR-0016).** Simple, but contradicts the
  chosen architecture and does not scale past one context's worth of screens. Rejected: the user
  directed us to establish Nx libs now.
- **B — Tag feature libs `context:shared` so the app can import them.** Avoids `context:web`, but
  collapses frontend context isolation entirely (identity UI could import organization UI). Rejected.
- **C — Frontend libs per context + `context:web` for the app (chosen).** Preserves per-context
  isolation for libraries while honestly modelling the single-app reality at exactly one project.

## Consequences

- `eslint.config.mjs` gains the frontend `type:*` rules and the `context:web` rule; `apps/web` is
  retagged `context:web`. `CLAUDE.md`'s tag-taxonomy section is updated in the same change.
- New frontend libs are generated with `@nx/angular:library … --unitTestRunner=vitest-analog` and
  carry exactly two tags (one `type:*`, one `context:*`), matching the backend convention.
- The boundary lint now enforces frontend layering and keeps cross-context UI coupling out of
  everything except the app.
- A second test runner (`vitest-analog`) now exists alongside the app's `@angular/build:unit-test`.
  Accepted as the supported way to test non-buildable Angular libs; revisit if Angular's
  application unit-test builder gains library support.

## Amendment — 2026-06-11: `frontend/` relocation and `data-access` → `api`

Two structural refinements supersede the paths and type names above:

1. **All Angular/Nx projects moved under `frontend/`.** Libraries now live at
   `frontend/libs/<context>/<type>` and the app at `frontend/apps/web`; `libs/` and `apps/` hold
   only .NET projects. Import aliases (`@roomy/<context>-<type>`) and Nx tags are unchanged by the
   move — only directory paths and the path-relative config (tsconfig/eslint/vite depth,
   `tsconfig.base.json` aliases, `pnpm-workspace.yaml`, CI generated-client paths) shifted.

2. **Per-context typed API-client libs renamed `data-access` → `api`.** The libs holding the
   generated OpenAPI client + gateway facade are now `frontend/libs/<context>/api`, project
   `<context>-api`, alias `@roomy/<context>-api`, Nx tag **`type:api`**. Rationale: `api` names what
   the lib is (one context's REST client) and disambiguates it from `shared/data-access`, which
   holds client-side data utilities (session, theme, pagination) and **keeps** `type:data-access`.
   So the frontend now carries both `type:api` and `type:data-access`.

   Updated frontend dependency rule (`eslint.config.mjs`): `feature → feature, ui, api, data-access,
   util`; `ui → ui, util`; `api → api, data-access, util`; `data-access → data-access, util`. The
   app (`type:app`) may additionally depend on `api`. `shared/data-access` is the sole remaining
   `type:data-access` lib.
