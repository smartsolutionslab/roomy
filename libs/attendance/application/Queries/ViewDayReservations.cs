using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// Intent to view a company-day's reservations (FR-012 view part, scenario 11). Any authenticated
// employee may view, so there is no actor here — only the company-day to replay.
public sealed record ViewDayReservations(CompanyIdentifier Company, BookingDate Date)
    : IQuery<IReadOnlyList<ReservationView>>;
