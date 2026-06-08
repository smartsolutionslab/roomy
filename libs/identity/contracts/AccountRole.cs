namespace SmartSolutionsLab.Roomy.Contracts.Identity;

// The role an account holds, as published to other contexts. Administrator implies the Employee
// baseline (FR-001/FR-002); the enum is flattened for the wire.
public enum AccountRole
{
    Employee,
    Administrator,
}
