using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Contracts.Organization;

// Organization's published language: emitted when an office is created (ADR-0031/0037). The attendance
// context consumes it to name the office on its local Rooms read model (003 US2). A minimal, versioned
// contract of IDs and primitives — no domain value objects.
public sealed record OfficeOpened(
    Guid OfficeId,
    Guid CompanyId,
    string Name,
    string Location,
    DateTimeOffset OccurredAt) : IIntegrationEvent;
