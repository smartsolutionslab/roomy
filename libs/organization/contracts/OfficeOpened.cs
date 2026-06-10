using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

public sealed record OfficeOpened(
    Guid OfficeId,
    Guid CompanyId,
    string Name,
    string Location,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
