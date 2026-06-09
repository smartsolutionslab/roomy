using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

// The messaging edge of the actor→employee feed (ADR-0031, 003 US4): Wolverine delivers organization's
// EmployeeHired through the durable inbox and this consumer mirrors the User<->Employee link onto
// attendance's local Employees read model, so the reserve/cancel endpoints can resolve the acting user
// to their EmployeeId and the occupancy view can name booked employees (004 US6, FR-007). The id link is a
// one-time fact, but the display name can change, so this is an idempotent upsert: a redelivery refreshes the
// name in place. The role and email are ignored.
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
