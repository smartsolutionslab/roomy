using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// One live reservation in an occupancy query's range: which employee holds a place in which room on
// which day, with the employee's display name. The handler counts these per (room, day) and shows the
// names only for today and the following day (FR-007).
public sealed record OccupantRecord(
    BookingDate Date,
    RoomIdentifier Room,
    EmployeeIdentifier Employee,
    string EmployeeName);
