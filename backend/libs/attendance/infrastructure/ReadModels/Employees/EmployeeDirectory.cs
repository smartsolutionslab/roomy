using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

public sealed class EmployeeDirectory(AttendanceDbContext context) : IEmployeeDirectory
{
    public async Task<Result<EmployeeIdentifier>> FindByUserAsync(UserIdentifier user, CancellationToken cancellationToken)
    {
        var link = await context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.UserId == user.Value, cancellationToken)
            .ConfigureAwait(false);

        return link is null
            ? Error.NotFound("unknown_employee", "The user is not known to the attendance service yet.")
            : EmployeeIdentifier.From(link.EmployeeId);
    }
}
