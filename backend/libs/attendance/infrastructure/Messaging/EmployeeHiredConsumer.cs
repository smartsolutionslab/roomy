using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class EmployeeHiredConsumer(AttendanceDbContext context)
{
    public async Task Handle(EmployeeHired message, CancellationToken cancellationToken)
    {
        var existing = await context.Employees.FindAsync([message.EmployeeId], cancellationToken);

        if (existing is null)
        {
            context.Employees.Add(new Employee
            {
                EmployeeId = message.EmployeeId,
                UserId = message.UserId,
                DisplayName = message.DisplayName,
            });
        }
        else
        {
            existing.DisplayName = message.DisplayName;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
