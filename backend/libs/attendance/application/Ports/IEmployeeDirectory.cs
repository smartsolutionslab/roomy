using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IEmployeeDirectory
{
    Task<Result<EmployeeIdentifier>> FindByUserAsync(UserIdentifier user, CancellationToken cancellationToken);
}
