namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;

// The office projection (contract: organization-api.md). Capacity is the derived sum of the rooms'
// capacities.
public sealed record OfficeResponse(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    IReadOnlyList<RoomResponse> Rooms);
