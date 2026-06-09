using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The IEmployeeDirectory adapter over the Employees read model (003 US4): it resolves the acting user
// (the token sub) to their EmployeeId from attendance's local mirror, fed by EmployeeHired
// (ADR-0014/0031), never by a cross-service join. A user whose EmployeeHired has not arrived is
// unknown_employee.
public sealed class EmployeeDirectory(AttendanceDbContext context) : IEmployeeDirectory
{
    public async Task<Result<EmployeeIdentifier>> FindByUserAsync(
        UserIdentifier user,
        CancellationToken cancellationToken)
    {
        var link = await context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.UserId == user.Value, cancellationToken)
            .ConfigureAwait(false);

        return link is null
            ? Error.NotFound("unknown_employee", "The user is not known to the attendance service yet.")
            : EmployeeIdentifier.From(link.EmployeeId);
    }
}
