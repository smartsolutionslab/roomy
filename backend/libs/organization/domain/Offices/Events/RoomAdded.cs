using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices.Events;

public sealed record RoomAdded(
    RoomIdentifier Room,
    OfficeIdentifier Office,
    CompanyIdentifier Company,
    RoomName Name,
    Capacity Capacity) : IDomainEvent;
