using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

// The IMyReservationsReadModel adapter (004 US9, ADR-0038): it reads the employee's rows from the
// Reservations projection, joined to the local Offices/Rooms read models for their names — all
// attendance's own read models, never a cross-service join (ADR-0014). Keyset-paginated by day
// (ADR-0044): the one-reservation-per-employee-per-day invariant makes the day a unique total order,
// so the cursor is the day alone — a plain `date > @cursor` that the (employee_id, date) index serves.
// Office/room names default to empty if their feed has not arrived.
public sealed class MyReservationsReadModel(AttendanceDbContext context) : IMyReservationsReadModel
{
    public async Task<Result<Page<MyReservationView>>> GetAsync(
        EmployeeIdentifier employee,
        PageRequest request,
        CancellationToken cancellationToken)
    {
        var decoded = request.DecodeCursor<ReservationCursor>();
        if (decoded.IsFailure)
        {
            return decoded.Error;
        }

        var employeeId = employee.Value;
        var reservations = context.Reservations.AsNoTracking()
            .Where(reservation => reservation.EmployeeId == employeeId);
        if (decoded.Value is { } after)
        {
            var afterDate = after.Date;
            reservations = reservations.Where(reservation => reservation.Date > afterDate);
        }

        var rows = await (
            from reservation in reservations
            join office in context.Offices.AsNoTracking()
                on reservation.OfficeId equals office.OfficeId into offices
            from office in offices.DefaultIfEmpty()
            join room in context.Rooms.AsNoTracking()
                on reservation.RoomId equals room.RoomId into rooms
            from room in rooms.DefaultIfEmpty()
            orderby reservation.Date
            select new
            {
                reservation.ReservationId,
                reservation.OfficeId,
                OfficeName = office != null ? office.Name : string.Empty,
                reservation.RoomId,
                RoomName = room != null ? room.Name : string.Empty,
                reservation.Date,
            })
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Page<MyReservationView>.FromProbe(
            rows,
            request.Limit,
            row => new MyReservationView(
                ReservationIdentifier.From(row.ReservationId),
                OfficeIdentifier.From(row.OfficeId),
                row.OfficeName,
                RoomIdentifier.From(row.RoomId),
                row.RoomName,
                BookingDate.From(row.Date)),
            row => new ReservationCursor(row.Date));
    }
}
