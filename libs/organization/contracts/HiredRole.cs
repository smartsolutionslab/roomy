namespace SmartSolutionsLab.Roomy.Contracts.Organization;

// The role the organization assigns when hiring. Administrator implies the Employee elevation, in
// keeping with the identity model (FR-001/FR-002) — the consumer maps it onto its own role.
public enum HiredRole
{
    Employee,
    Administrator,
}
