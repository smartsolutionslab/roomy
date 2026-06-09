using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

// The messaging edge of the actor→employee feed (ADR-0031, 003 US4): Wolverine delivers organization's
// EmployeeHired through the durable inbox and this consumer mirrors the User<->Employee link onto
// attendance's local Employees read model, so the reserve/cancel endpoints can resolve the acting user
// to their EmployeeId. The link is a one-time fact, so a redelivery is a no-op (idempotent). Only the
// EmployeeId/UserId are needed here — role, email and the rest of the contract are ignored.
public sealed class EmployeeHiredConsumer(AttendanceDbContext context)
{
    public async Task Handle(EmployeeHired message, CancellationToken cancellationToken)
    {
        var existing = await context.Employees
            .FindAsync([message.EmployeeId], cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            context.Employees.Add(new Employee
            {
                EmployeeId = message.EmployeeId,
                UserId = message.UserId,
            });

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
