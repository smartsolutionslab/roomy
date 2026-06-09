using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Reads an employee's own reservations from the local Reservations read model, joined to Rooms/Offices
// for their names (ADR-0038), never a cross-service join (ADR-0014). An employee with no reservations
// yields an empty list — absence is not an error here.
public interface IMyReservationsReadModel
{
    Task<IReadOnlyList<MyReservationView>> GetAsync(EmployeeIdentifier employee, CancellationToken cancellationToken);
}
