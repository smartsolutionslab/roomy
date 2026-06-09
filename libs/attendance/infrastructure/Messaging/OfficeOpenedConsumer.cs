using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

// The messaging edge of the office-name feed (ADR-0031, 004): Wolverine delivers organization's
// OfficeOpened through the durable inbox and this consumer mirrors the office name onto attendance's
// local Offices read model, so the occupancy office rollup can name the office without joining to
// organization's database (ADR-0014). The upsert is idempotent, so an at-least-once redelivery is
// harmless. This is the only place OfficeOpened is referenced — the application layer never sees
// organization's published language.
public sealed class OfficeOpenedConsumer(AttendanceDbContext context)
{
    public async Task Handle(OfficeOpened message, CancellationToken cancellationToken)
    {
        var existing = await context.Offices
            .FindAsync([message.OfficeId], cancellationToken).ConfigureAwait(false);

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
