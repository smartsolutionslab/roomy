using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class OfficeOpenedConsumer(AttendanceDbContext context)
{
    public Task Handle(OfficeOpened message, CancellationToken cancellationToken) =>
        context.UpsertAsync(
            message.OfficeId,
            () => new Office
            {
                OfficeId = message.OfficeId,
                CompanyId = message.CompanyId,
                Name = message.Name,
            },
            office => office.Name = message.Name,
            cancellationToken);
}
