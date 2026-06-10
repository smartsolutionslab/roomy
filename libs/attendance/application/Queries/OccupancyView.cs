using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// One day's occupancy for the requested scope: the office rollup and each room's figure (FR-001/002).
// A range query returns one of these per day in the range (scenario 3); past days are included as
// read-only history (FR-009).
public sealed record OccupancyView(
    BookingDate Date,
    OfficeOccupancy Office,
    IReadOnlyList<RoomOccupancy> Rooms);
