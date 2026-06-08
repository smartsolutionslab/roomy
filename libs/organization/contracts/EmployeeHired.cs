using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

// Organization's published language: emitted when an employee is hired, and the trigger for the
// identity context to provision the account (ADR-0025, ADR-0031). The UserId is pre-allocated by the
// hiring side and is the correlation key for the 1:1 User<->Employee link, so identity provisions the
// account under exactly this identifier. The initial password is a transient secret set in Keycloak,
// never persisted. A minimal, versioned contract of IDs and primitives — no domain value objects.
public sealed record EmployeeHired(
    Guid EmployeeId,
    Guid UserId,
    string Email,
    string DisplayName,
    HiredRole Role,
    string InitialPassword,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
