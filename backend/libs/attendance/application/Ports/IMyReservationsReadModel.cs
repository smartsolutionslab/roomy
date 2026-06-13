using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IMyReservationsReadModel
{
    Task<Page<MyReservationView>> GetAsync(EmployeeIdentifier employee, PageRequest request, CancellationToken cancellationToken);
}
