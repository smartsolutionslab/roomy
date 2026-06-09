namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;

// One row per live reservation — the materialised projection target for the occupancy read side
// (004, ADR-0038). It is maintained inline, in the same transaction that appends the AttendanceDay
// events: ReservationPlaced inserts a row, ReservationCancelled deletes it. From these rows the
// occupancy figures (per-room and the office rollup) are a GROUP BY, "my reservations" is a filter by
// employee, and the today/tomorrow names join the Employees read model. It carries no invariant — a
// rebuildable cache derived from the event log.
public sealed class Reservation
{
    public required Guid ReservationId { get; init; }

    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Guid OfficeId { get; init; }

    public required Guid RoomId { get; init; }

    public required DateOnly Date { get; init; }
}
