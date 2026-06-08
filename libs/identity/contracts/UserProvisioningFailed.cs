using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Identity;

// Identity's published language: emitted when an account cannot be provisioned (e.g. Keycloak rejects
// the password or the email is taken). Drives compensation of the provisioning saga in the
// organization context (ADR-0025, ADR-0031). The EmployeeId correlates back to the saga.
public sealed record UserProvisioningFailed(
    Guid UserId,
    Guid EmployeeId,
    UserProvisioningFailureReason Reason,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
