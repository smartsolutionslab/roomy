using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;

public static class ReservationRangeQuery
{
    extension(IQueryable<Reservation> reservations)
    {
        public IQueryable<Reservation> WithinRange(BookingDateRange range)
        {
            var from = range.From.Value;
            var to = range.To.Value;
            return reservations.Where(reservation => reservation.Date >= from && reservation.Date <= to);
        }
    }
}
