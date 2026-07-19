# Research: Non-Vacuous Architecture Tests

## R1 — How to discover assemblies so a ProjectReference guarantees inspection

**Decision**: Enumerate `AppContext.BaseDirectory` for `SmartSolutionsLab.Roomy.*.dll`
and load each by simple name via `Assembly.Load(new AssemblyName(fileName))`, keeping
the existing executing-assembly exclusion. The AppDomain walk and the seed-assembly
reference-graph traversal are deleted — the directory scan strictly supersedes them
(every assembly they could find is copy-local in the output directory).

**Rationale**: The root cause is that a `ProjectReference` without a compile-time type
use never lands in the AssemblyRef table, so reference-graph traversal can never see
it. What a `ProjectReference` *does* guarantee is copy-local output next to the test
binary — so the output directory is the one place where "referenced by the test
project" and "discoverable" coincide. Empirically verified in this repo: the built test
dll references only the four seed assemblies while 12 further Roomy dlls sit unloaded
in `bin`.

`Assembly.Load(AssemblyName)` (by simple name) is chosen over
`Assembly.LoadFrom(path)` because it resolves through the default load context: an
assembly some test already loaded is returned, not loaded a second time from a path
(no duplicate `Assembly` identities, no `LoadFrom`-context type-identity surprises).
The default context probes the application base directory, which is exactly where the
dlls sit.

**Alternatives considered**:
- *`typeof` anchors per context assembly in `ArchitectureConventions`* — works, but
  reintroduces the failure mode with every future assembly: forgetting the anchor
  silently shrinks coverage again, and nothing fails. Rejected: the canary would have
  to enumerate the same list anyway, so the anchor list is pure duplication.
- *Keep the reference-graph walk and only add missing anchors* — same objection; also
  keeps ~40 lines of traversal whose only remaining purpose the directory scan already
  serves. Simplicity-first says delete it.
- *`Assembly.LoadFrom(path)`* — loads into the LoadFrom context; a type loaded there is
  not identical to the same type in the default context, which can split NetArchTest's
  view. Rejected in favor of load-by-name.

## R2 — Failure mode for an unloadable Roomy assembly

**Decision**: Remove the `catch (FileNotFoundException or BadImageFormatException)`
swallow. A `SmartSolutionsLab.Roomy.*.dll` that is present but cannot be loaded fails
discovery with an exception message naming the file (wrap the load in a try/catch that
rethrows with the assembly name in the message).

**Rationale**: FR-007. Silent skipping is the original defect in miniature: coverage
shrinks and nothing tells anyone. The swallow existed to tolerate reference-graph
entries whose dlls were absent; with directory-scan discovery, everything enumerated is
by definition present, so a load failure is always a real problem.

## R3 — Shape of the canary

**Decision**: A dedicated test file asserting that the discovered set contains each of
the 18 expected simple names (9 context layers, 2 contracts, shared-kernel,
application-contracts, persistence, messaging, cryptography, web-http,
infrastructure-authentication), with a Shouldly assertion that names any missing
assembly. Superset is allowed (a new assembly is picked up automatically and inspected;
adding it to the canary list is part of creating it, per the README's "adding a
context" step).

**Rationale**: FR-002/SC-003. Contains-each (not set-equality) keeps the failure signal
precise: a *dropped* expected assembly fails loudly, while a *new* assembly does not
spuriously fail an unrelated story — it is already inspected by the rules either way,
which is the property that matters. The README instructs authors of new contexts to
extend the canary, mirroring the existing convention.

**Alternatives considered**: exact set-equality — stricter, but makes every new library
story touch this test before anything else and adds no enforcement (discovery already
inspects new assemblies without registration). Rejected as ceremony.

## R4 — What replaces the dormant escape hatches

**Decision**: In `LayerDependencyConventionTests.AssertConvention` and
`CrossContextIsolationConventionTests`, a zero inspected-type count becomes a hard
failure (`matchedTypes.ShouldBeGreaterThan(0, ...)`), and the now-false "dormant /
layers do not exist yet" documentation comments are deleted along with the stale
HONESTY NOTE. The per-rule "types inspected" counting stays — it is what makes the new
assertion possible.

**Rationale**: FR-003. The hatch was honest when the layers were empty; today it is the
mechanism of the silent pass. With the contexts implemented, zero matches can only mean
discovery is broken — exactly the condition that must fail.

## R5 — Risk: newly effective rules surface real violations

**Decision**: Proceed; expectation is green. The Nx module-boundary lint mirrors the
same dependency and isolation rules at project granularity and is green, and the
contracts libraries live in the neutral `SmartSolutionsLab.Roomy.Contracts.*` namespace
(ADR-0031), so infrastructure-level contract consumption does not trip the isolation
rule (which matches `SmartSolutionsLab.Roomy.<Context>` prefixes). If a violation
nevertheless appears: mechanical fixes land in this story; anything structural becomes
its own follow-up story with the failure documented — never a rule suppression
(spec Assumptions, FR-005).
