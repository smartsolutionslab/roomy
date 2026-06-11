using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

public sealed record RoomAdded(
    Guid RoomId,
    Guid OfficeId,
    Guid CompanyId,
    string Name,
    int Capacity,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
