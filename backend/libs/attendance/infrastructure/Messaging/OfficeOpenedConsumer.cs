using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class OfficeOpenedConsumer(AttendanceDbContext context)
{
    public async Task Handle(OfficeOpened message, CancellationToken cancellationToken)
    {
        var existing = await context.Offices.FindAsync([message.OfficeId], cancellationToken);

        if (existing is null)
        {
            context.Offices.Add(new Office
            {
                OfficeId = message.OfficeId,
                CompanyId = message.CompanyId,
                Name = message.Name,
            });
        }
        else
        {
            existing.Name = message.Name;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
