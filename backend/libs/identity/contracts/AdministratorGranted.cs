using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Identity;

public sealed record AdministratorGranted(
    Guid UserId,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
