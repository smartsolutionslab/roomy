using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Identity;

public sealed record UserProvisioningFailed(
    Guid UserId,
    Guid EmployeeId,
    UserProvisioningFailureReason Reason,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
