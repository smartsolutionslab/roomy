# 0056. Navigation derived from the router config, with role inferred from the route guard

- **Status:** Accepted
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

The signed-in SPA presents the same set of destinations twice — the sidebar (`apps/web/.../app.html`)
and the home dashboard cards (`home.html`). Both hand-write the link list and wrap the
administrator-only entries in `@if (user.roles.includes('administrator'))`. Meanwhile the Angular router
already declares every one of these routes and already guards them: `authGuard` for any signed-in
employee, `authGuard + adminGuard` for administrators (`identity`/`organization`/`attendance` route files).

So "which destinations exist" lives in three places and "who may see each" lives in three places, and they
can drift: a route can be re-guarded as admin-only while the nav keeps showing it to everyone, or a new
admin page can be added to the router but forgotten in one of the two menus. The role rule has no single
owner.

How should the navigation be expressed so the link list and its visibility cannot diverge from the routes
they point at?

## Decision drivers

- **Single source of truth.** The route already owns its path and its access rule (the guard). Navigation
  should be a projection of that, not a parallel copy.
- **No drift between "can navigate" and "is offered".** A user must never be shown a link that its guard
  would bounce to `/not-authorized`.
- **Two consumers, one model.** Sidebar and dashboard should render the same data, differently styled.
- **Boundaries (ADR-0035).** The builder must stay within `feature → feature/ui/data-access/util` and must
  not import another context's libs.
- **Simplicity (CLAUDE.md).** Minimum machinery; no speculative role engine.

## Considered options

- **A — Standalone `NavItem[]` list.** A declarative array in a shared lib, with `roles` per item, mapped
  to `@for`. Removes the template duplication but *restates* the route paths and the role rule — a fourth
  copy that can still drift from the guards. (This was the initial direction, then rejected.)
- **B — BFF-served navigation manifest.** The gateway computes the per-user menu and the SPA fetches it.
  Truly server-authoritative, but adds an endpoint, a contract, a drift-gated client, and a round trip for
  data the SPA already has (the session roles) — disproportionate to the problem.
- **C — Derive from `Router.config`, infer role from the guard (chosen).** Attach presentation-only
  metadata to routes; read the config at runtime; infer admin-only from the presence of `adminGuard` in
  the route's effective `canActivate`.

## Decision

**Option C.** Navigation is a runtime projection of the router configuration:

1. **Presentation metadata on the route.** A navigable route carries `data.nav: NavMeta` —
   `{ labelKey, icon, order, descKey? }` — and nothing about roles. `NavMeta` (and the resolved `NavItem`)
   live in `@roomy/shared-feature`; route files import the type alongside the guards they already import.
2. **`NavigationService` (in `@roomy/shared-feature`).** Injects `Router` and `SessionService`. It walks
   `Router.config` recursively, accumulating ancestor path segments and ancestor `canActivate`. For each
   route bearing `data.nav` it emits a `NavItem` with the full `path` and `requiresAdmin` set when the
   effective guard chain **includes the `adminGuard` function reference**. The eager parts of the config
   (path, `data`, `canActivate`) are all present synchronously even though components are `loadComponent`
   lazy, so no navigation or route activation is needed to build the menu.
3. **Role inference, not declaration.** `requiresAdmin` comes from the guard, so the link's visibility and
   the route's actual access rule are the *same fact*. There is no `roles` field to keep in sync.
4. **Role-filtered signal API.** The service exposes computed signals — `items()` (all the current user may
   see, ordered by `order`), `mainItems()` and `adminItems()` (the same split into the self-service group
   and the administrator group) — derived from `SessionService.currentUser()`.
5. **Two renderers, one model.** The sidebar renders `mainItems()` then, under the existing administration
   heading, `adminItems()`; the home dashboard renders `items()` as cards (using `descKey`). Neither
   template restates the destination list or the role rule.

## Consequences

**Positive**
- One source of truth: a route's existence, its path, and its access rule define its navigation entry.
  Adding/guarding a route updates both menus automatically; a link can't outlive its guard.
- No new endpoint, contract, or round trip; roles come from the already-loaded session signal.
- The builder is pure config-reading and stays inside `context:shared`/`type:feature`; it never imports a
  context's libs, so module boundaries hold (it sees other contexts only through the runtime config the app
  composition root assembles).

**Negative / trade-offs**
- The admin check is a **function-reference comparison** (`canActivate.includes(adminGuard)`). It relies on
  every route using the one exported `adminGuard`; a hand-rolled equivalent guard would not be recognised.
  Acceptable: `adminGuard` is the single shared admin gate, and an architecture/unit test covers the
  inference.
- Display order is an explicit `order` on each route, spread across three context route files; it is the
  one piece of cross-file coordination (the router's own concatenation order is identity → organization →
  attendance, which is not the desired visual order).
- Presentation metadata now rides on routes. It is namespaced under `data.nav` and typed by `NavMeta`, so
  it stays clearly separate from routing behaviour.

**Follow-ups**
- No Nx taxonomy change. `NavMeta`/`NavigationService` sit in the existing `type:feature`/`context:shared`
  rules.
- If a third role appears later, the inference generalises by reading the corresponding guard; only then
  would a richer role model (beyond the binary employee/administrator) be warranted.
