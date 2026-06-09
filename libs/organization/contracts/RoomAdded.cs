using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

// Organization's published language: emitted when a room is added to an office (ADR-0031/0037). It is
// the capacity feed the attendance context needs to enforce no-overbooking (003 US2, FR-004/FR-007):
// attendance mirrors Capacity onto its local Rooms read model. Capacity is the number of places (>= 1).
// A minimal, versioned contract of IDs and primitives — no domain value objects.
public sealed record RoomAdded(
    Guid RoomId,
    Guid OfficeId,
    Guid CompanyId,
    string Name,
    int Capacity,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
