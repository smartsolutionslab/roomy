using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// An office's occupancy rollup for a day: the sum of its rooms' occupied places against the sum of their
// capacities (e.g. 12/30, FR-002), and whether the office is full — all its rooms at capacity (FR-008).
public sealed record OfficeOccupancy(
    OfficeIdentifier Office,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull);
