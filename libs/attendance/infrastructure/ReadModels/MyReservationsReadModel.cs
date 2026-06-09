using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;

// The IMyReservationsReadModel adapter (004 US9, ADR-0038): it reads the employee's rows from the
// Reservations projection, joined to the local Offices/Rooms read models for their names — all
// attendance's own read models, never a cross-service join (ADR-0014). Results are ordered by day so the
// list reads past → future; office/room names default to empty if their feed has not arrived.
public sealed class MyReservationsReadModel(AttendanceDbContext context) : IMyReservationsReadModel
{
    public async Task<IReadOnlyList<MyReservationView>> GetAsync(
        EmployeeIdentifier employee,
        CancellationToken cancellationToken)
    {
        var employeeId = employee.Value;

        var rows = await (
            from reservation in context.Reservations.AsNoTracking()
            where reservation.EmployeeId == employeeId
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
            }).ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => new MyReservationView(
                ReservationIdentifier.From(row.ReservationId),
                OfficeIdentifier.From(row.OfficeId),
                row.OfficeName,
                RoomIdentifier.From(row.RoomId),
                row.RoomName,
                BookingDate.From(row.Date)))
            .ToList();
    }
}
