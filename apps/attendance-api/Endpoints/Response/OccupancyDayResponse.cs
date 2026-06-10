namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record OccupancyDayResponse(
    DateOnly Date,
    OfficeOccupancyResponse Office,
    IReadOnlyList<RoomOccupancyResponse> Rooms);
