using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Views a company-day's reservations by replaying the AttendanceDay stream (research R6) — no separate
// read model this slice (the occupancy projection is 004). A never-booked day replays to an empty
// aggregate, so the view is naturally an empty list, never "not found".
public sealed class ViewDayReservationsHandler(IAttendanceDayRepository attendanceDays)
    : IQueryHandler<ViewDayReservations, IReadOnlyList<ReservationView>>
{
    public async Task<Result<IReadOnlyList<ReservationView>>> HandleAsync(
        ViewDayReservations query,
        CancellationToken cancellationToken)
    {
        var attendanceDay = await attendanceDays
            .LoadAsync(query.Company, query.Date, cancellationToken).ConfigureAwait(false);

        var reservations = attendanceDay.Reservations
            .Select(reservation => new ReservationView(
                reservation.Id,
                reservation.Office,
                reservation.Room,
                attendanceDay.Date,
                reservation.Employee))
            .ToList();

        return Result.Success<IReadOnlyList<ReservationView>>(reservations);
    }
}
