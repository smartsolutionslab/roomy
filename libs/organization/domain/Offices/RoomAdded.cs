using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// Raised when a room is added to an office (ADR-0032). Intra-context and framework-free — it carries
// the room's value objects plus its office and company. The infrastructure unit of work drains it at
// commit and maps it to the RoomAdded integration contract: the capacity feed the attendance context
// enforces no-overbooking against (ADR-0037, 003 US2).
public sealed record RoomAdded(
    RoomIdentifier Room,
    OfficeIdentifier Office,
    CompanyIdentifier Company,
    RoomName Name,
    Capacity Capacity) : IDomainEvent;
