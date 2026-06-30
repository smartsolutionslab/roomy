using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

public sealed record EmployeeHired(
    Guid EmployeeId,
    Guid UserId,
    string Email,
    string DisplayName,
    HiredRole Role,
    string EncryptedInitialPassword,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
