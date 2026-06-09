namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// Where an employee is in the provisioning saga (ADR-0025): freshly hired and awaiting its login
// (Provisioning), provisioned and usable (Active), or unable to be provisioned (Failed — the
// compensation). Active and Failed are terminal for a given hire; the state machine makes the
// no-half-account guarantee (FR-007) an enforced invariant.
public enum ProvisioningState
{
    Provisioning,
    Active,
    Failed,
}
