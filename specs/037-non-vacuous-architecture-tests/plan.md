# Implementation Plan: Architecture Tests Genuinely Inspect Every Roomy Assembly

**Branch**: `037-non-vacuous-architecture-tests` | **Date**: 2026-07-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/037-non-vacuous-architecture-tests/spec.md`

## Summary

The NetArchTest suite discovers assemblies via the AppDomain plus the compiled
reference graph of four seed assemblies. Because no test code uses types from the
context projects, the compiler drops their `ProjectReference`s from the AssemblyRef
table, so all nine context layer assemblies (plus both contracts libraries and
cryptography) are never loaded — every convention rule inspects zero context types and
the "dormant" escape hatches turn that into a silent green pass. Fix: discover by
scanning the test output directory for `SmartSolutionsLab.Roomy.*.dll` (a
`ProjectReference` guarantees copy-local, which then guarantees inspection), fail
loudly on unloadable Roomy assemblies, pin the expected assembly set with a canary
test, remove the dormant escape hatches, and add the two missing project references
(`web-http`, `infrastructure-authentication`). GitHub issue #226.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: NetArchTest.Rules, xunit.v3, Shouldly (all already in the project)

**Storage**: N/A

**Testing**: `dotnet test` on `backend/tests/architecture/Roomy.ArchitectureTests`

**Target Platform**: test-host only (no production code shipped)

**Project Type**: test infrastructure (quality gate)

**Performance Goals**: N/A — suite runtime stays in the seconds range

**Constraints**: no rule bodies change; only assembly discovery, the expected-set
canary, and the zero-type failure mode change. If newly effective rules surface real
violations, the *violating code* is fixed (or split into a follow-up story if
structural), never the rules.

**Scale/Scope**: one test project, four files touched, one new test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec-Driven & Test-First**: PASS — spec exists with testable criteria; the Red
  step is naturally expressed: the canary test and the de-dormanted rules fail against
  current discovery before the fix lands.
- **II. Clean Architecture & DDD**: PASS — this feature *restores enforcement* of the
  principle; no layer boundaries are touched.
- **III. Context Isolation**: PASS — no cross-context code; the isolation rule becomes
  genuinely enforced.
- **IV. No Framework in the Core**: PASS — test project only.
- **V. Decisions Are Recorded**: PASS — no architectural decision changes; the
  discovery mechanism is an implementation detail of an existing, ADR-backed gate
  (ADR-0002/0003; `backend/tests/architecture/README.md` is updated in the same change
  because its "loaded assemblies" caveat changes).
- **VI. Green Before Done**: PASS — full verify suite runs at the end; known risk that
  enforcement surfaces violations is handled by fixing code, not suppressing rules.
- **VII. Small, Single-Purpose Changes**: PASS — one story, one branch, atomic commits.

## Project Structure

### Documentation (this feature)

```text
specs/037-non-vacuous-architecture-tests/
├── spec.md
├── plan.md              # This file
├── research.md          # Phase 0: discovery/load-mechanism decisions
├── quickstart.md        # Phase 1: how to validate the gate end-to-end
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

`data-model.md` and `contracts/` are intentionally absent: the feature has no domain
entities and no external interface — it is an internal quality gate.

### Source Code (repository root)

```text
backend/tests/architecture/Roomy.ArchitectureTests/
├── Roomy.ArchitectureTests.csproj        # + web-http, + infrastructure-authentication refs; stale comments refreshed
├── RoomyAssemblies.cs                    # discovery rewritten: scan output dir, load, fail loudly
├── RoomyAssembliesTests.cs               # NEW: canary pinning the expected assembly set
├── LayerDependencyConventionTests.cs     # dormant zero-type branch → hard failure; stale docs removed
├── CrossContextIsolationConventionTests.cs # same
└── (other rule files unchanged)

backend/tests/architecture/README.md      # discovery caveat + "adding a context" step updated
```

**Structure Decision**: everything stays inside the existing architecture test project;
no production project changes are expected (contingency: mechanical fixes in context
code if a rule surfaces a real violation — see spec Assumptions).

## Complexity Tracking

No constitution violations; table not needed.
