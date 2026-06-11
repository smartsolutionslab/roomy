using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record OccupancyView(
    BookingDate Date,
    OfficeOccupancy Office,
    IReadOnlyList<RoomOccupancy> Rooms);
