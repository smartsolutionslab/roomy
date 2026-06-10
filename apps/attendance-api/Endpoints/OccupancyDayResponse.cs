namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints;

internal sealed record OccupancyDayResponse(
    DateOnly Date,
    OfficeOccupancyResponse Office,
    IReadOnlyList<RoomOccupancyResponse> Rooms);
