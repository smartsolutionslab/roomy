# Feature Specification: Role-Based Navigation (router-config-driven)

**Feature Branch:** `feat/014-role-based-navigation`
**Status:** Draft
**Created:** 2026-06-11
**Updated:** 2026-06-11
**Realizes:** FR-003 (administrator-only sections) for the SPA shell — a refactor that makes the
signed-in navigation a single, role-filtered model derived from the Angular router configuration,
replacing the hardcoded link/card lists duplicated in the sidebar and the home dashboard.

## Summary

The authenticated SPA shows the same set of destinations in two places: the **sidebar** (`app.html`)
and the **home dashboard cards** (`home.html`). Today both restate, by hand, *which* destinations exist
and *who* may see them — each wraps its administrator links in `@if (user.roles.includes('administrator'))`.
The Angular router already declares these routes and already guards them (`authGuard` for any signed-in
employee, `authGuard + adminGuard` for administrators). The role rule therefore exists in **three** places
that can drift apart.

This feature collapses them to one source of truth: the **routes**. Each navigable route carries
presentation metadata in its `data.nav` (label key, icon, display order, optional dashboard description
key); its access rule stays exactly where it already is — the `canActivate` guard it declares. A
`NavigationService` (in `@roomy/shared-feature`) reads `Router.config` at runtime, selects the routes
that have `data.nav`, computes each route's full path, and **infers** whether a route is administrator-only
by inspecting its guards (a route guarded by `adminGuard` is administrator-only). It exposes role-filtered,
ordered, signal-based lists that the sidebar and the dashboard both render. A navigation link can no longer
diverge from who is actually allowed through the route, because both come from the one guard.

No backend change, no new endpoint, no new translation strings, no new icons. This is a pure structural
refactor of how the existing navigation is expressed. It is standalone/signal-based/zoneless/OnPush
(ADR-0016/0035), localized (DE + EN, Transloco, ADR-0024), and accessible (WCAG 2.2 AA, ADR-0024). The
role projection comes from the BFF session (`CurrentUser.roles`, ADR-0013/0030); no token reaches the SPA.

> **Not here:** no change to routing behaviour, route guards, or which destinations exist; no change to
> the public (signed-out) top bar; no backend/gateway change. The set of links shown and who sees them is
> identical to today — only how that set is expressed changes.

## User Scenarios & Testing

### Primary User Story

As a signed-in employee, I want the navigation to show exactly the destinations I'm allowed to use, so I
am never offered a link that would only send me to a "not authorized" page.

### Acceptance Scenarios

1. **Employee sees the self-service destinations**
   - GIVEN a signed-in user whose roles are `['employee']`
   - WHEN the shell renders the sidebar
   - THEN it shows Reserve, My reservations, Occupancy, and Calendar, in that order
   - AND it shows no administration section and no administrator-only link

2. **Administrator additionally sees the administration section**
   - GIVEN a signed-in user whose roles include `administrator`
   - WHEN the shell renders the sidebar
   - THEN it shows the four self-service destinations
   - AND, under the administration section heading, On behalf, Offices, and Administration (users)

3. **The dashboard mirrors the sidebar from the same model**
   - GIVEN a signed-in user
   - WHEN the home dashboard renders its cards
   - THEN the cards are exactly the destinations that user's role grants, in the same order as the sidebar,
     each with its title and description

4. **Visibility follows the route guard, not a restated role**
   - GIVEN a route guarded by `adminGuard`
   - WHEN navigation is built for a non-administrator
   - THEN that route's entry is absent from the navigation
   - AND GIVEN the same route for an administrator, its entry is present

5. **Only routes that opt in appear**
   - GIVEN a routed destination with no `data.nav` metadata (e.g. `/account`, a redirect)
   - WHEN navigation is built
   - THEN that route contributes no navigation entry

6. **Nested route paths resolve to their full link**
   - GIVEN the attendance children are mounted under the parent path `attendance`
   - WHEN navigation is built
   - THEN the Reserve entry links to `/attendance/reserve` (parent segment + child segment)

### Edge Cases

- **Signed-out / no session:** the signed-in shell (and therefore the navigation) is not rendered at all;
  the public top bar is shown instead. Navigation is only ever built for a decided, signed-in session.
- **A route declares `data.nav` but no guard:** it is treated as visible to any signed-in user (not
  administrator-only). (Not expected in the current config — every navigable route is guarded — but the
  builder must not assume a guard is present.)
- **Ordering across context route files:** display order is the explicit `order` in `data.nav`, not the
  declaration order in `Router.config` (which concatenates identity, organization, then attendance).

## Requirements

### Functional Requirements

- **FR-N1** Navigation entries MUST be derived at runtime from `Router.config`; the only routes that
  appear are those carrying `data.nav` presentation metadata.
- **FR-N2** A route's required role MUST be inferred from its effective `canActivate` guards (including
  guards inherited from ancestor routes): a route reached through `adminGuard` is administrator-only; any
  other navigable route is available to every signed-in user.
- **FR-N3** The navigation MUST be filtered by the current user's roles (`CurrentUser.roles`): an
  administrator-only entry is shown only when the session's roles include `administrator`.
- **FR-N4** Each entry MUST expose its full router path (accumulating ancestor path segments), its label
  translation key, its icon, its display order, and an optional dashboard description key.
- **FR-N5** Entries MUST be ordered by their declared `order`, and administrator-only entries MUST be
  presented as a distinct group under the existing administration section heading in the sidebar.
- **FR-N6** Both the sidebar and the home dashboard MUST render from this one model; neither may restate
  the role rule or the destination list inline.
- **FR-N7** The set of destinations shown to each role, their labels, icons, order, and the dashboard
  descriptions MUST be unchanged from the pre-refactor behaviour.

### Non-Functional / Constraints

- The `NavigationService` and the `NavMeta` type live in `@roomy/shared-feature` (`type:feature`,
  `context:shared`); context route files attach `data.nav` by importing the type from there. No Nx module
  boundary is crossed (`feature → feature/ui/data-access/util`; the service reads the runtime router
  config, it does not import another context's libs).
- No new HTTP calls; roles come from the already-loaded `SessionService` signal.
- All strings via Transloco (existing `shell.*` and `home.cards.*` keys); no hardcoded text.

## Key Entities

- **NavMeta** — presentation metadata attached to a route's `data.nav`: `labelKey`, `icon` (`IconName`),
  `order`, optional `descKey`.
- **NavItem** — a resolved navigation entry produced by the builder: full `path`, `labelKey`, `icon`,
  optional `descKey`, and `requiresAdmin` (inferred from the route's guards).

## Review & Acceptance Checklist

- [ ] Every acceptance scenario has a test written before its implementation.
- [ ] Sidebar and dashboard both render from `NavigationService`; no inline role checks remain in either
      template for the navigation/cards.
- [ ] Role visibility is inferred from the route guard; no role is restated in `data.nav`.
- [ ] The links shown per role, their order, labels, icons, and dashboard descriptions match the
      pre-refactor screens exactly.
- [ ] DE + EN render correctly; no hardcoded strings; no a11y regressions.
- [ ] ADR-0050 recorded; all quality gates green; no suppressions.
