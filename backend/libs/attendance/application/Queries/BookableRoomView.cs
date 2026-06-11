using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record BookableRoomView(
    OfficeIdentifier Office,
    string OfficeName,
    RoomIdentifier Room,
    string RoomName,
    RoomCapacity Capacity);
