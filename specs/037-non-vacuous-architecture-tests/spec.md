# Feature Specification: Architecture Tests Genuinely Inspect Every Roomy Assembly

**Feature Branch**: `037-non-vacuous-architecture-tests`

**Created**: 2026-07-19

**Status**: Draft

**Input**: User description: "Architecture tests must genuinely inspect every Roomy assembly (fix vacuous pass, GitHub issue #226). The NetArchTest suite in backend/tests/architecture discovers assemblies via AppDomain plus the compiled reference graph of four seed assemblies; because no test code uses types from the context projects, the compiler drops their ProjectReferences from the AssemblyRef table and the identity/organization/attendance domain/application/infrastructure assemblies (plus contracts and cryptography) are never loaded — the layer-dependency, cross-context-isolation, and no-MediatR rules inspect zero context types and the 'dormant' escape hatches (written when the layers were empty) turn that into a silent green pass."

## Problem Statement

The architecture test suite is the .NET-side enforcement of the project's non-negotiable
dependency rule (constitution Principle II), context isolation (Principle III), and
framework ban (Principle IV). Today it enforces none of them for the three bounded
contexts: assembly discovery only finds assemblies reachable from four shared "seed"
assemblies' compiled reference graphs, and since none of those reference any context
assembly, every identity/organization/attendance layer assembly sits unloaded next to
the test binary. The convention rules therefore inspect zero context types, and an
escape hatch written when the contexts were empty ("dormant until context projects
exist") converts that emptiness into a green pass. The contexts have been fully
implemented since; the gate has been silently off the whole time.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Architecture violations in context code fail the gate (Priority: P1)

A developer (or agent) introduces a forbidden dependency in a bounded context — for
example, a domain type referencing an infrastructure type, an application type using a
banned framework, or one context referencing another context's types. Running the
architecture test suite fails with a message naming the offending types.

**Why this priority**: This is the entire purpose of the suite; it is currently not
delivered at all for context code. Every other golden rule leans on this gate.

**Independent Test**: Temporarily introduce a violation in a context assembly (e.g., a
domain type depending on an application type), run the architecture suite, and observe
a failure naming that type; revert, and observe green.

**Acceptance Scenarios**:

1. **Given** the implemented context assemblies, **When** the architecture suite runs,
   **Then** every layer/isolation/framework rule inspects a non-zero number of context
   types.
2. **Given** a context type that violates the dependency rule, **When** the suite runs,
   **Then** the corresponding rule fails and names the type.
3. **Given** the current codebase contains no violations, **When** the suite runs,
   **Then** it passes — and if enforcing the rules surfaces real existing violations,
   those are fixed in the code, never suppressed in the rules.

---

### User Story 2 - A silently dropped assembly fails loudly (Priority: P2)

A future change removes or renames a context project, or a build change stops an
assembly from being copied next to the test binary. The suite fails with a message
naming the missing assembly instead of quietly shrinking its coverage.

**Why this priority**: The current defect is precisely a silent coverage shrink; without
a canary, the same failure mode can recur unnoticed.

**Independent Test**: Remove one expected assembly name from the discovery result (or
simulate a missing file) and observe the canary test fail naming it.

**Acceptance Scenarios**:

1. **Given** the expected set of Roomy assemblies, **When** discovery finds all of them,
   **Then** the canary test passes.
2. **Given** an expected assembly that discovery cannot find, **When** the canary test
   runs, **Then** it fails and names the missing assembly.

---

### User Story 3 - Web plumbing and authentication libraries join the inspected set (Priority: P3)

The shared web plumbing (`web-http`) and authentication (`infrastructure-authentication`)
libraries — which carry real infrastructure-adjacent code — are inspected by the same
convention rules as every other Roomy assembly.

**Why this priority**: Smaller surface than the three contexts, but currently not even
referenced by the test project, so no future fix to discovery alone would cover them.

**Independent Test**: The canary test's expected set includes both assemblies; the suite
fails if either is absent.

**Acceptance Scenarios**:

1. **Given** the fixed discovery, **When** the suite runs, **Then** both assemblies are
   loaded and inspected, and the applicable rules (e.g., no MediatR) evaluate their
   types.

### Edge Cases

- A non-Roomy assembly in the output directory must not be loaded by discovery
  (only `SmartSolutionsLab.Roomy.*` assemblies are in scope).
- An assembly present both in the already-loaded set and on disk must be inspected
  exactly once (no duplicate inspection, no load conflict).
- A Roomy assembly on disk that fails to load (corrupt/incompatible) must produce a
  loud failure, not be silently skipped — silent skipping recreates the original defect.
- Attendance has no contracts library today; the expected set must reflect the
  assemblies that actually exist (and grow when new ones are added — the canary should
  make a *missing expected* assembly fail, while discovery picks up *new* assemblies
  automatically).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Assembly discovery MUST find and load every Roomy assembly that is
  present in the test run's output directory, so that a project reference from the test
  project guarantees inspection.
- **FR-002**: A canary test MUST assert that the discovered set contains each expected
  assembly by name: the three contexts' domain/application/infrastructure assemblies
  (nine), the identity and organization contracts assemblies, the shared kernel,
  application contracts, persistence, messaging, cryptography, web plumbing
  (`web-http`), and authentication (`infrastructure-authentication`) assemblies —
  eighteen in total today.
- **FR-003**: The dormant/empty-layer escape hatches in the layer-dependency and
  cross-context-isolation tests MUST be removed; a rule that inspects zero types MUST
  fail, not pass.
- **FR-004**: The test project MUST reference the `web-http` and
  `infrastructure-authentication` projects so their assemblies are available for
  inspection.
- **FR-005**: Any real architecture violation surfaced by the newly effective rules
  MUST be fixed in the offending code; rules MUST NOT be weakened, scoped down, or
  suppressed to get green.
- **FR-006**: Discovery MUST ignore non-Roomy assemblies and MUST NOT inspect the test
  assembly itself.
- **FR-007**: A Roomy assembly that is found but cannot be loaded MUST fail the suite
  with a message naming the assembly.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every convention rule (layer dependency, cross-context isolation,
  no-MediatR/framework bans) reports inspecting a non-zero number of types from each
  bounded context.
- **SC-002**: A deliberately introduced dependency violation in any context layer fails
  the suite (demonstrated once during Red, per the work loop).
- **SC-003**: Removing any expected assembly from the discovered set fails exactly one
  clearly named canary test.
- **SC-004**: The full verify suite (build with warnings-as-errors, all tests,
  formatting) is green on completion.

## Assumptions

- The nine context layer projects, two contracts projects, and the shared libraries
  already referenced by the test project remain the complete set of production Roomy
  assemblies today; attendance intentionally has no contracts library yet.
- Discovering assemblies from the test output directory is acceptable because the test
  host copies all project references there; no test-runner in use isolates tests from
  their output directory.
- The existing per-assembly rule bodies (what each rule checks) are correct and stay
  unchanged; this feature fixes *which assemblies* they see, not *what* they assert.
- If enforcing the rules reveals real violations, fixing them belongs to this story
  only when small and mechanical; a structural violation would become its own follow-up
  story with this suite temporarily documenting the known failure — decided with the
  reviewer at that point (none are currently known or expected: the Nx boundary lint
  mirrors these rules on project level and is green).
