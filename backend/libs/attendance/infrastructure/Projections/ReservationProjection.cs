using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays.Events;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using ReservationRow = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations.Reservation;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Projections;

public sealed class ReservationProjection(AttendanceDbContext context) : IReservationProjection
{
    public async Task ApplyAsync(IReadOnlyList<object> events, CancellationToken cancellationToken)
    {
        foreach (var streamEvent in events)
        {
            switch (streamEvent)
            {
                case ReservationPlaced placed:
                    context.Reservations.Add(new ReservationRow
                    {
                        ReservationId = placed.ReservationId,
                        CompanyId = placed.CompanyId,
                        EmployeeId = placed.EmployeeId,
                        OfficeId = placed.OfficeId,
                        RoomId = placed.RoomId,
                        Date = placed.Date,
                    });
                    break;

                case ReservationCancelled cancelled:
                    var row = await context.Reservations.FindAsync([cancelled.ReservationId], cancellationToken).ConfigureAwait(false);
                    if (row is not null)
                    {
                        context.Reservations.Remove(row);
                    }

                    break;
            }
        }
    }
}
