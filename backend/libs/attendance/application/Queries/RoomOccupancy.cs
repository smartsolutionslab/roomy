using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record RoomOccupancy(
    RoomIdentifier Room,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull,
    IReadOnlyList<Occupant>? Occupants);
