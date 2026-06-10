using System.Text.Json.Serialization;

namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record RoomOccupancyResponse(
    Guid RoomId,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<OccupantResponse>? Occupants);
