using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Application.UseCases;

// Intent to view a keyset-paginated page of an employee's own reservations across all time (FR-004,
// scenario 6; ADR-0044). The employee is the acting user, resolved from the token at the endpoint; the
// page lists their reservations in day order, past and future, each with its office, room, and day.
public sealed record ViewMyReservations(EmployeeIdentifier Employee, PageRequest Page)
    : IQuery<Page<MyReservationView>>;
