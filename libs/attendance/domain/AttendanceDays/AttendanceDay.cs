using SmartSolutionsLab.Roomy.SharedKernel;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The attendance context's aggregate root and consistency boundary (ADR-0026): one instance per
// company calendar day, identified by CompanyId + Date. It enforces both reservation invariants in
// one place — no-overbooking per (room, day) and one reservation per employee per day — plus the
// bookable-day rule. State is the fold over the stream (EventSourcedAggregate): Reserve raises a
// ReservationPlaced that Apply folds in. Capacity and "today" are supplied by the caller (research
// R3/R4), so the aggregate reads no master data and no clock.
public sealed class AttendanceDay : EventSourcedAggregate
{
    private readonly List<Reservation> reservations = [];

    private AttendanceDay(CompanyIdentifier company, BookingDate date)
    {
        Company = company;
        Date = date;
    }

    public CompanyIdentifier Company { get; }

    public BookingDate Date { get; }

    public IReadOnlyCollection<Reservation> Reservations => reservations;

    // The way to obtain an instance: the repository builds the day for a (company, date) and replays
    // its stream onto it via LoadFromHistory. A never-booked day is simply an empty one — there is no
    // "not found" (research R2).
    public static AttendanceDay For(CompanyIdentifier company, BookingDate date) => new(company, date);

    public Result<ReservationIdentifier> Reserve(
        EmployeeIdentifier employee,
        RoomReference room,
        RoomCapacity capacity,
        BookingDate today,
        DateTimeOffset occurredAt)
    {
        if (!BookingWindow.IsBookable(Date, today))
        {
            return Error.Validation(
                "not_bookable",
                "Only working days within the next two weeks can be reserved.");
        }

        if (reservations.Exists(reservation => reservation.Employee == employee))
        {
            return Error.Conflict(
                "already_reserved_today",
                "The employee already holds a reservation for this day.");
        }

        var placesTaken = reservations.Count(reservation => reservation.Room == room.Room);
        if (placesTaken >= capacity.Value)
        {
            return Error.Conflict("room_full", "The room has no remaining places for this day.");
        }

        var reservationId = ReservationIdentifier.New();
        Raise(new ReservationPlaced(
            reservationId.Value,
            Company.Value,
            Date.Value,
            employee.Value,
            room.Office.Value,
            room.Room.Value,
            occurredAt));

        return reservationId;
    }

    protected override void Apply(object @event)
    {
        if (@event is ReservationPlaced placed)
        {
            reservations.Add(new Reservation(
                ReservationIdentifier.From(placed.ReservationId),
                EmployeeIdentifier.From(placed.EmployeeId),
                OfficeIdentifier.From(placed.OfficeId),
                RoomIdentifier.From(placed.RoomId)));
        }
    }
}
