using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// One employee's guaranteed place in one room on one day — an entity inside the AttendanceDay
// aggregate, never persisted on its own. It exists only as the replay result of the stream
// (ADR-0012), so it is created by the aggregate (internal) and carries no behaviour of its own; the
// invariants live in the aggregate.
public sealed class Reservation : IEntity
{
    internal Reservation(
        ReservationIdentifier id,
        EmployeeIdentifier employee,
        OfficeIdentifier office,
        RoomIdentifier room)
    {
        Id = id;
        Employee = employee;
        Office = office;
        Room = room;
    }

    public ReservationIdentifier Id { get; }

    public EmployeeIdentifier Employee { get; }

    public OfficeIdentifier Office { get; }

    public RoomIdentifier Room { get; }
}
