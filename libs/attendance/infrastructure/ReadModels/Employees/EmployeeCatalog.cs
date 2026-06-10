using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The IEmployeeCatalog adapter (009): lists the employees from attendance's local Employees read model,
// ordered by display name for the on-behalf picker — attendance's own read model, fed by EmployeeHired
// (ADR-0014/0031), never a cross-context join.
public sealed class EmployeeCatalog(AttendanceDbContext context) : IEmployeeCatalog
{
    public async Task<IReadOnlyList<EmployeeView>> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.DisplayName)
            .Select(employee => new { employee.EmployeeId, employee.DisplayName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new EmployeeView(EmployeeIdentifier.From(row.EmployeeId), row.DisplayName))
            .ToList();
    }
}
