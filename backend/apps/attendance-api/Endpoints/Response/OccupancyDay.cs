namespace SmartSolutionsLab.Roomy.Attendance.Api.Endpoints.Response;

internal sealed record OccupancyDay(
    DateOnly Date,
    OfficeOccupancy Office,
    IReadOnlyList<RoomOccupancy> Rooms);
