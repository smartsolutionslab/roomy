using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public sealed record OccupancyData(
    OfficeIdentifier Office,
    string OfficeName,
    IReadOnlyList<RoomDescriptor> Rooms,
    IReadOnlyList<OccupantRecord> Occupants);
