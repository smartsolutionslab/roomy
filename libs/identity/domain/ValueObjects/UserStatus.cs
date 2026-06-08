namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// Account lifecycle: Provisioning until the Keycloak user exists and the role is assigned, then
// Active (loginable). No deactivate/delete in the MVP (out of scope). The User aggregate owns the
// Provisioning -> Active transition.
public enum UserStatus
{
    Provisioning,
    Active,
}
