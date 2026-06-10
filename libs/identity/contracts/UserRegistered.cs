using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Identity;

public sealed record UserRegistered(
    Guid UserId,
    Guid EmployeeId,
    string Email,
    AccountRole Role,
    Guid KeycloakSubjectId,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
