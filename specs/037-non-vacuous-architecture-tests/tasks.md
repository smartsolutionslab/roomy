# Tasks: Architecture Tests Genuinely Inspect Every Roomy Assembly

**Input**: Design documents from `specs/037-non-vacuous-architecture-tests/`
**Prerequisites**: plan.md, research.md, spec.md, quickstart.md

**Tests**: This feature *is* test infrastructure — the constitution's Red step is
expressed by watching the reworked suite fail against the current discovery before
fixing it. No separate test tasks exist beyond the suite itself.

**Organization**: Tasks are grouped by user story. US1 (rules bite) and US2 (canary)
share one Green step — the discovery rewrite — so US2's red assertion is demonstrated
via the no-build dll-removal procedure from quickstart.md.

## Phase 1: Setup

- [ ] T001 Add `ProjectReference`s for `backend/libs/web-http/src/Roomy.Web.Http.csproj` and `backend/libs/infrastructure-authentication/src/Roomy.Infrastructure.Authentication.csproj` to `backend/tests/architecture/Roomy.ArchitectureTests/Roomy.ArchitectureTests.csproj`, and refresh the file's stale "layers are empty today / rules activate as types are added" comments to describe the copy-local-guarantees-inspection contract.

## Phase 2: Foundational

*(none — the feature touches a single test project; Setup is the only prerequisite)*

## Phase 3: User Story 1 — Architecture violations in context code fail the gate (P1)

**Goal**: Every convention rule inspects real context types; a zero-type rule run is a
failure, not a pass.

**Independent Test**: Introduce a transient dependency violation in a context layer;
the suite fails naming the type. Revert; green.

- [ ] T002 [US1] **Red** — Remove the dormant zero-type escape hatches: in `backend/tests/architecture/Roomy.ArchitectureTests/LayerDependencyConventionTests.cs` replace the `matchedTypes == 0` dormant branch with `matchedTypes.ShouldBeGreaterThan(0, ...)` and delete the stale HONESTY NOTE / dormant doc comments; same for the zero-type branch in `CrossContextIsolationConventionTests.cs`. Run `dotnet test backend/tests/architecture/Roomy.ArchitectureTests` and **record the failures** (all layer + isolation tests must now fail with zero inspected types — proving the vacuous pass).
- [ ] T003 [US1] **Green** — Rewrite `Discover()` in `backend/tests/architecture/Roomy.ArchitectureTests/RoomyAssemblies.cs`: enumerate `AppContext.BaseDirectory` for `SmartSolutionsLab.Roomy.*.dll`, load each by simple name via `Assembly.Load(new AssemblyName(...))`, keep the executing-assembly exclusion and ordinal ordering, delete the AppDomain walk + seed-reference-graph traversal, and replace the `FileNotFoundException`/`BadImageFormatException` swallow with a rethrow naming the assembly (research R1/R2). Suite green.
- [ ] T004 [US1] **Prove the gate bites (SC-002, transient)** — Add a deliberate violation in a context layer (e.g. an EF Core reference in `backend/libs/attendance/domain`), rebuild, observe the corresponding rule fail naming the type, then revert. Nothing is committed from this task; note the observed failure message in the PR description.

## Phase 4: User Story 2 — A silently dropped assembly fails loudly (P2)

**Goal**: The expected assembly set is pinned; a drop-out fails one clearly named test.

**Independent Test**: Delete one context dll from the test output dir and re-run with
`--no-build`; the canary fails naming it.

- [ ] T005 [US2] Create `backend/tests/architecture/Roomy.ArchitectureTests/RoomyAssembliesTests.cs` — a canary test asserting `RoomyAssemblies.All` contains each of the 18 expected simple names (9 context layers, `Roomy.Contracts.Identity`/`.Organization`, shared-kernel, application-contracts, persistence, messaging, cryptography, web-http, infrastructure-authentication), with a Shouldly failure message naming any missing assembly (research R3: contains-each, not set-equality).
- [ ] T006 [US2] Validate the canary's failure mode per `quickstart.md`: delete `SmartSolutionsLab.Roomy.Attendance.Domain.dll` from the output directory, run the canary with `--no-build`, confirm it fails naming the assembly; restore (rebuild).

## Phase 5: User Story 3 — Web plumbing and authentication join the inspected set (P3)

**Goal**: `web-http` and `infrastructure-authentication` are genuinely inspected.

**Independent Test**: Canary includes both names (T005); rules evaluate their types.

- [ ] T007 [US3] Confirm both new assemblies are discovered and inspected (canary green after T001+T003+T005) and that the newly effective rules stay green across all 18 assemblies; if any real violation surfaces, apply the research R5 policy (mechanical fix here, structural → follow-up story; never a rule suppression).

## Phase 6: Polish & Cross-Cutting

- [ ] T008 [P] Update `backend/tests/architecture/README.md`: replace the "inspects every *loaded* assembly / reference alone is not enough" caveat with the new contract (ProjectReference → copy-local → discovered), and update the "adding a new context" step to include extending the canary list.
- [ ] T009 Run the full verify gates: `dotnet build backend -warnaserror`, `dotnet test backend`, `dotnet format --verify-no-changes` (JS gates untouched by this feature). Fix anything surfaced; no suppressions.

## Dependencies

- T001 → T005 (canary expects the two new assemblies) and T007.
- T002 (Red) precedes T003 (Green) — constitution Principle I.
- T003 greens both T002's failures and enables T005/T006/T007.
- T008 parallelizable with T005–T007; T009 last.

## Implementation Strategy

Single PR, atomic commits in task order (e.g. `test(architecture): fail on zero
inspected types`, `test(architecture): discover assemblies from the output directory`,
`test(architecture): pin the expected assembly set`, `docs(architecture): update
discovery caveat`). MVP = US1 (T001–T004); US2/US3 complete the drop-out protection.
