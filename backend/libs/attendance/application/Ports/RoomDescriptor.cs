using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public sealed record RoomDescriptor(
    RoomIdentifier Room,
    string RoomName,
    RoomCapacity Capacity);
