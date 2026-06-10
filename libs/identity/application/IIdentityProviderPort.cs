using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application;

// Port to the external identity provider (Keycloak), which owns credentials while the identity
// context owns account/role data (ADR-0013, research R1/R2). This is an external-system port (not
// a repository), so it stays in the application layer. The infrastructure layer implements it with
// the Keycloak admin API.
//
// Provisioning failures (e.g. a rejected password or a taken email) are expected business outcomes
// and are returned as a failed Result, never thrown. The initial password is a transient transport
// value to Keycloak — not domain state — so it crosses the port as a string.
public interface IIdentityProviderPort
{
    Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
        Email email,
        DisplayName displayName,
        string initialPassword,
        Role role,
        CancellationToken cancellationToken);

    // Assigns the administrator realm role to an already-provisioned subject (US4 / IA-4). Keycloak is
    // the token authority, so an elevation must propagate here for the role to appear on the user's
    // token. Idempotent — re-assigning an existing role mapping is a no-op at Keycloak.
    Task<Result> AssignAdministratorRoleAsync(KeycloakSubjectIdentifier subject, CancellationToken cancellationToken);
}
