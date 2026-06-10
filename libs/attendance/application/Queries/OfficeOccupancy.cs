using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record OfficeOccupancy(
    OfficeIdentifier Office,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull);
