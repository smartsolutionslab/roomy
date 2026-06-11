using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class RoomAddedConsumer(AttendanceDbContext context)
{
    public async Task Handle(RoomAdded message, CancellationToken cancellationToken)
    {
        var existing = await context.Rooms.FindAsync([message.RoomId], cancellationToken);

        if (existing is null)
        {
            context.Rooms.Add(new Room
            {
                RoomId = message.RoomId,
                OfficeId = message.OfficeId,
                CompanyId = message.CompanyId,
                Capacity = message.Capacity,
                Name = message.Name,
            });
        }
        else
        {
            existing.Capacity = message.Capacity;
            existing.Name = message.Name;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
