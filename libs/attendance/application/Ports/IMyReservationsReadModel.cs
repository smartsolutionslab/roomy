using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Reads a keyset-paginated page of an employee's own reservations from the local Reservations read
// model, joined to Rooms/Offices for their names (ADR-0038), never a cross-service join (ADR-0014).
// Ordered by day; the day is a unique key per employee (one reservation per day), so it is the cursor.
// An employee with no reservations yields an empty page; a malformed cursor is a validation failure.
public interface IMyReservationsReadModel
{
    Task<Result<Page<MyReservationView>>> GetAsync(
        EmployeeIdentifier employee, PageRequest request, CancellationToken cancellationToken);
}
