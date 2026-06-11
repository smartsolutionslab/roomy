# 0058. Feature-local presentational components, with the page as the single smart orchestrator

- **Status:** Proposed
- **Date:** 2026-06-11
- **Deciders:** Heiko Weiß

## Context and problem statement

`organization/feature`'s `OfficesPage` is a single 235-line smart component. It mixes three
concerns: orchestration (loading offices, calling the gateway, mapping HTTP errors to result and
feedback signals), the page-level form/editor state (one `createForm`, one shared `textForm`, one
shared `roomForm`, and a single `activeEditor` so only one inline editor is open at a time), and a
large, repeated per-office template fragment (~110 of the 160 template lines render *one* office and
are repeated with `@for`). The repeated fragment is what makes the component hard to read and test in
isolation.

No feature page in the repo has yet been split into child components — every `frontend/libs/<context>/feature`
folder is currently exactly one page (verified across identity, organization, attendance). And the
frontend has only one presentational library, `@roomy/shared-ui` (ADR-0048), which holds the
*design-system* primitives (Button, Card, FormField, …); `organization` has no `ui` library at all.

So splitting `OfficesPage` forces two decisions that no prior ADR settles:

1. **What owns the editing state** once a per-office fragment becomes its own component — does each
   card own its forms/editor, or does the page keep them?
2. **Where the extracted components live** — co-located in the `feature` lib, or promoted into a new
   `organization/ui` lib?

This ADR fixes a reusable pattern for both, so the next page split (offices is the first of several
identified candidates: reserve, occupancy, on-behalf) does not re-litigate them.

## Decision drivers

- **Behaviour preservation.** This is a refactor under a green bar (CLAUDE.md work loop step 6). The
  17 existing `OfficesPage` tests are the contract; the visible behaviour — including
  *single-editor-at-a-time* — must not change.
- **Simplicity first (CLAUDE.md).** Minimum machinery. No new library, tag, or abstraction unless a
  second consumer actually needs it.
- **Boundaries (ADR-0035).** Whatever we do must stay inside `feature → feature/ui/data-access/util`
  and must not cross an Nx boundary or a context boundary.
- **Testability.** Each extracted piece should be unit-testable in isolation (given inputs → renders;
  on interaction → emits), without standing up the whole page or stubbing the gateway.
- **Clear smart/dumb seam.** One obvious place owns side effects (HTTP, state); the rest is pure
  rendering + intent events.

## Considered options

- **A — Leave `OfficesPage` as one component.** No structural change. Rejected: the repeated 110-line
  fragment stays un-isolatable, the page stays at 235 lines, and the identified follow-on splits have
  no pattern to follow.
- **B — Promote the children into a new `organization/ui` library.** A "proper" presentational lib per
  context. Rejected *for now*: the components serve exactly one page and are not reused across pages or
  contexts; a new Nx project, its tags, its test setup, and a public barrel are disproportionate to the
  need. ADR-0048 already reserves `shared/ui` for cross-context design-system primitives, which these
  domain-shaped components (an *office* card, a *room* row) are not. Premature.
- **C — Self-contained children that own their editing state.** Each `OfficeCard` holds its own
  `textForm`/`roomForm`/editor and emits only domain intents (rename, relocate, add room). The cleanest
  smart/dumb separation. Rejected: it changes behaviour — independent per-card state means several cards
  could be editing at once, which the current page deliberately prevents with one shared `activeEditor`.
  That is a behaviour change, not a refactor, and would need its own spec and new tests.
- **D — Feature-local presentational children; the page stays the single smart orchestrator (chosen).**

## Decision

**Option D.** Extract presentational child components that are **co-located in the feature library**,
while the page remains the one smart component.

1. **Co-location, not a new lib.** Presentational children that serve a single feature page live beside
   that page inside its `frontend/libs/<context>/feature` folder (e.g.
   `organization/feature/src/lib/offices/office-card.ts`). They are *not* exported from the lib's public
   barrel — they are private to the page. An intra-library child component crosses no Nx boundary; the
   `feature` tag already permits a feature lib to contain its own components.

2. **The page is the only smart component.** `OfficesPage` keeps every side effect and all shared state:
   the injected gateway and all HTTP calls, the `createForm`/`textForm`/`roomForm` form groups, the
   single `activeEditor` signal, and the `result`/conflict/failed/not-found feedback signals. The
   children inject nothing, call no gateway, and own no cross-cutting state.

3. **Children are pure presentation + intent.** Each child is `standalone`, `OnPush`, uses signal
   `input()`/`output()` only, no `NgModule`, no lifecycle side effects (ADR-0016/0035). It receives the
   data and editor/feedback state it must display via `input()` (including the shared `FormGroup` it
   binds, when it hosts an inline editor) and reports user intent via `output()`; the page reacts. Visual
   state such as "is *this* office's rename editor open" is derived inside the child as a pure function of
   its inputs, not pushed down call-by-call.

4. **Shared editor state stays singular.** Because the forms and `activeEditor` remain on the page and
   are passed *down*, only one editor is open at a time exactly as today. The refactor is observably a
   no-op.

5. **Promotion rule (when co-location stops being right).** A presentational child is promoted out of the
   feature lib — into `<context>/ui` (created then) or into `shared/ui` if truly cross-context — only when
   a **second** page or context needs it. First reuse is the trigger; speculation is not.

## Consequences

**Positive**
- The page template shrinks to its orchestration shell plus `<roomy-office-card>` / `<roomy-create-office-form>`;
  the 110-line per-office fragment becomes one focused, independently testable component.
- Each child has a small, explicit contract (its inputs/outputs), unit-testable without the gateway.
- No new library, Nx tag, or public API; no boundary moves; no contract/codegen impact. The diff is
  contained to one feature folder.
- A clear, repeatable recipe for the next page splits (reserve, occupancy, on-behalf).

**Negative / trade-offs**
- Because the page keeps the state, the children receive comparatively **many** inputs — including shared
  `FormGroup`s bound by a child that hosts an inline editor. Passing a `FormGroup` into a "presentational"
  component is a deliberate compromise: it is the price of preserving the single-editor behaviour without
  the distributed state of option C. The seam stays clean in the direction that matters — no child does
  I/O or owns shared state.
- Co-located, non-exported children are invisible outside the page, so cross-page reuse requires the
  explicit promotion step above. Accepted: that step is cheap and is exactly when a `ui` lib earns its
  keep.

**Follow-ups**
- No Nx taxonomy change; the `type:feature`/`context:organization` rules are unchanged.
- The first application is spec `019-offices-page-components`. If/when a child is reused, create
  `organization/ui` under ADR-0035's rules and move it there in that slice.
