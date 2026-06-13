using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class EmployeeHiredConsumer(AttendanceDbContext context)
{
    public Task Handle(EmployeeHired message, CancellationToken cancellationToken) =>
        context.UpsertAsync(
            message.EmployeeId,
            () => new Employee
            {
                EmployeeId = message.EmployeeId,
                UserId = message.UserId,
                DisplayName = message.DisplayName,
            },
            employee => employee.DisplayName = message.DisplayName,
            cancellationToken);
}
