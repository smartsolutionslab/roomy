using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// A room's occupancy for a day: the occupied places against the room's capacity (e.g. 3/8, FR-001), and
// whether it is full (occupied == capacity, FR-008). Occupants is the booked employees by name, present
// only for today and the following day (FR-007); on other days it is null and only the counts are shown.
public sealed record RoomOccupancy(
    RoomIdentifier Room,
    string Name,
    int Occupied,
    int Capacity,
    bool IsFull,
    IReadOnlyList<Occupant>? Occupants);
