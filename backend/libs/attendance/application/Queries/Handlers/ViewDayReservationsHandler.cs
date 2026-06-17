using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;

public sealed class ViewDayReservationsHandler(IAttendanceDayRepository attendanceDays)
    : IQueryHandler<ViewDayReservations, IReadOnlyList<ReservationView>>
{
    public async Task<Result<IReadOnlyList<ReservationView>>> HandleAsync(ViewDayReservations query, CancellationToken cancellationToken)
    {
        var (company, date) = query;

        var attendanceDay = await attendanceDays.LoadAsync(company, date, cancellationToken);

        IReadOnlyList<ReservationView> reservations = attendanceDay.Reservations
            .Select(reservation => new ReservationView(
                reservation.Identifier,
                reservation.Office,
                reservation.Room,
                attendanceDay.Date,
                reservation.Employee))
            .ToList();

        return Result.Success(reservations);
    }
}
