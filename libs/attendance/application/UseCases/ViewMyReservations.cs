using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to view an employee's own reservations across all time (FR-004, scenario 6). The employee is
// the acting user, resolved from the token at the endpoint; the result lists every reservation they
// hold, past and future, each with its office, room, and day.
public sealed record ViewMyReservations(EmployeeIdentifier Employee)
    : IQuery<IReadOnlyList<MyReservationView>>;
