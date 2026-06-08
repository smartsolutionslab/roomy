<!--
One story per PR, small enough to review in one sitting. See CONTRIBUTING.md.
Commits are not squashed — each must be a clean, atomic Conventional Commit.
-->

## Summary

<!-- What does this PR do and why? Keep it focused on intent and domain correctness. -->

## Linked issue / spec

<!-- Use a closing keyword so the issue closes on merge, e.g. "Closes #123". -->

- Closes #
- Spec: <!-- e.g. specs/001-identity-access/spec.md, or N/A for docs/tooling -->

## How it was verified

<!-- Briefly: which tests cover this, what you ran, what you observed. -->

## Quality gates

<!-- All must be green before requesting review; CI enforces the same. -->

- [ ] Tests added/updated for each acceptance criterion (written before the implementation) and passing
- [ ] `dotnet build Roomy.slnx -warnaserror` clean (nullable on, analyzers on, no warnings)
- [ ] `dotnet test Roomy.slnx` green (unit + integration + architecture tests; coverage floor met)
- [ ] `dotnet format --verify-no-changes` clean
- [ ] `pnpm nx run-many -t lint` clean (ESLint + Nx module-boundary lint)
- [ ] ADR added under `docs/adr/` if the change is architectural
- [ ] Docs updated (`CLAUDE.md`, `CONTRIBUTING.md`, README, ADRs) if a convention changed
- [ ] No unjustified suppressions or skipped tests (any suppression carries an inline justification)

<!-- Merge with "Rebase and merge". Delete the branch afterwards. -->
