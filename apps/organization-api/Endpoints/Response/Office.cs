namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;

public sealed record Office(
    Guid Id,
    string Name,
    string Location,
    int Capacity,
    IReadOnlyList<Room> Rooms);
