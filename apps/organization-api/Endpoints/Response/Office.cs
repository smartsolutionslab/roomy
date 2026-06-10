namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;

// The office projection (contract: organization-api.md). Capacity is the derived sum of the rooms'
// capacities.
public sealed record Office(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    IReadOnlyList<Room> Rooms);
