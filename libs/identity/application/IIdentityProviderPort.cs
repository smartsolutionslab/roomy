using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Application;

// Port to the external identity provider (Keycloak), which owns credentials while the identity
// context owns account/role data (ADR-0013, research R1/R2). The application defines it; the
// infrastructure layer implements it with the Keycloak admin API.
//
// Provisioning failures (e.g. a rejected password or a taken email) are expected business outcomes
// and are returned as a failed Result, never thrown. The initial password is a transient transport
// value to Keycloak — not domain state — so it crosses the port as a string.
public interface IIdentityProviderPort
{
    Task<Result<KeycloakSubjectId>> ProvisionUserAsync(
        Email email,
        DisplayName displayName,
        string initialPassword,
        Role role,
        CancellationToken cancellationToken);
}
