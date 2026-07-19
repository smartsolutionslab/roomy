# Quickstart: Validating the Non-Vacuous Architecture Tests

## Prerequisites

- .NET 10 SDK
- Repository restored (`dotnet restore` happens implicitly)

## Run the gate

```powershell
dotnet test backend/tests/architecture/Roomy.ArchitectureTests
```

**Expected**: all tests pass, including the new `RoomyAssembliesTests` canary.

## Prove discovery is genuine (SC-001, SC-003)

1. Run only the canary:

   ```powershell
   dotnet test backend/tests/architecture/Roomy.ArchitectureTests --filter RoomyAssembliesTests
   ```

   **Expected**: pass, having asserted all 18 expected assembly names are discovered.

2. Simulate a silent drop-out: delete one context dll from the test output directory
   (e.g. `bin/Debug/net10.0/SmartSolutionsLab.Roomy.Attendance.Domain.dll`) and re-run
   the canary **without rebuilding** (`--no-build`).

   **Expected**: the canary fails naming `SmartSolutionsLab.Roomy.Attendance.Domain`.

## Prove the rules bite (SC-002)

1. Add a temporary violation in a context layer, e.g. in
   `backend/libs/attendance/domain` reference a type from
   `SmartSolutionsLab.Roomy.Attendance.Application` (or add a `using` +
   field of an EF Core type).
2. Rebuild and run the suite.

   **Expected**: the corresponding layer rule fails and names the offending type —
   no dormant pass.
3. Revert the violation; the suite is green again.

## Full verify (SC-004)

```powershell
dotnet build backend -warnaserror
dotnet test backend
dotnet format --verify-no-changes
```

**Expected**: all green; no new suppressions anywhere in the diff.
