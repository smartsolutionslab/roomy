using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IMyReservationsReadModel
{
    Task<Result<Page<MyReservationView>>> GetAsync(
        EmployeeIdentifier employee, PageRequest request, CancellationToken cancellationToken);
}
