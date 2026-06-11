using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

public sealed class MyReservationsReadModel(AttendanceDbContext context) : IMyReservationsReadModel
{
    public async Task<Result<Page<MyReservationView>>> GetAsync(EmployeeIdentifier employee, PageRequest request, CancellationToken cancellationToken)
    {
        var decoded = request.DecodeCursor<ReservationCursor>();
        if (decoded.IsFailure) return decoded.Error;

        var employeeId = employee.Value;
        var reservations = context.Reservations.AsNoTracking().Where(reservation => reservation.EmployeeId == employeeId);
        if (decoded.Value is { } after)
        {
            var afterDate = after.Date;
            reservations = reservations.Where(reservation => reservation.Date > afterDate);
        }

        var rows = await reservations
            .GroupJoin(
                context.Offices.AsNoTracking(),
                reservation => reservation.OfficeId,
                office => office.OfficeId,
                (reservation, offices) => new { reservation, offices })
            .SelectMany(
                joined => joined.offices.DefaultIfEmpty(),
                (joined, office) => new { joined.reservation, office })
            .GroupJoin(
                context.Rooms.AsNoTracking(),
                joined => joined.reservation.RoomId,
                room => room.RoomId,
                (joined, rooms) => new { joined.reservation, joined.office, rooms })
            .SelectMany(
                joined => joined.rooms.DefaultIfEmpty(),
                (joined, room) => new { joined.reservation, joined.office, room })
            .OrderBy(row => row.reservation.Date)
            .Select(row => new
            {
                row.reservation.ReservationId,
                row.reservation.OfficeId,
                OfficeName = row.office != null ? row.office.Name : string.Empty,
                row.reservation.RoomId,
                RoomName = row.room != null ? row.room.Name : string.Empty,
                row.reservation.Date,
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
