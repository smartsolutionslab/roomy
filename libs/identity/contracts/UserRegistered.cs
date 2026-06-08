using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Identity;

// Identity's published language: emitted when an account is fully provisioned (Keycloak user created,
// role assigned, record persisted Active). Completes the identity step of the provisioning saga; the
// EmployeeId correlates back to it (ADR-0025, ADR-0031).
public sealed record UserRegistered(
    Guid UserId,
    Guid EmployeeId,
    string Email,
    AccountRole Role,
    Guid KeycloakSubjectId,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
