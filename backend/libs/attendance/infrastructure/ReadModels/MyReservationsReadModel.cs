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
