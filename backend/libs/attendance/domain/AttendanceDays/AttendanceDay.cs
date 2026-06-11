using SmartSolutionsLab.Roomy.SharedKernel;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

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
            return Error.Validation("not_bookable", "Only working days within the next two weeks can be reserved.");
        }

        if (reservations.Exists(reservation => reservation.Employee == employee))
        {
            return Error.Conflict("already_reserved_today", "The employee already holds a reservation for this day.");
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

    public Result Cancel(
        ReservationIdentifier reservation,
        EmployeeIdentifier actor,
        bool actorIsAdmin,
        BookingDate today,
        DateTimeOffset occurredAt)
    {
        var existing = reservations.Find(held => held.Identifier == reservation);
        if (existing is null)
        {
            return Error.NotFound("reservation_not_found", "No such reservation for this day.");
        }

        if (Date.Value < today.Value)
        {
            return Error.Validation("past_immutable", "A reservation in the past cannot be cancelled.");
        }

        if (!MayCancel(existing, actor, actorIsAdmin))
        {
            return Error.Forbidden("not_owner", "Only the reservation's owner or an administrator may cancel it.");
        }

        Raise(new ReservationCancelled(
            existing.Identifier.Value,
            Company.Value,
            Date.Value,
            existing.Employee.Value,
            existing.Room.Value,
            occurredAt));

        return Result.Success();
    }

    private static bool MayCancel(Reservation reservation, EmployeeIdentifier actor, bool actorIsAdmin) =>
        actorIsAdmin || reservation.Employee == actor;

    protected override void Apply(object @event)
    {
        switch (@event)
        {
            case ReservationPlaced placed:
                reservations.Add(new Reservation(
                    ReservationIdentifier.From(placed.ReservationId),
                    EmployeeIdentifier.From(placed.EmployeeId),
                    OfficeIdentifier.From(placed.OfficeId),
                    RoomIdentifier.From(placed.RoomId)));
                break;

            case ReservationCancelled cancelled:
                reservations.RemoveAll(held => held.Identifier == ReservationIdentifier.From(cancelled.ReservationId));
                break;
        }
    }
}
