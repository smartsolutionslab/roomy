using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

// The messaging edge of the capacity feed (ADR-0031/0037, 003 US2): Wolverine delivers organization's
// RoomAdded through the durable inbox and this consumer mirrors the room onto attendance's local Rooms
// read model so the reserve flow can enforce no-overbooking against real capacity. The upsert is
// idempotent, so an at-least-once redelivery is harmless. This is the only place RoomAdded is
// referenced — the application layer never sees organization's published language.
public sealed class RoomAddedConsumer(AttendanceDbContext context)
{
    public async Task Handle(RoomAdded message, CancellationToken cancellationToken)
    {
        var existing = await context.Rooms
            .FindAsync([message.RoomId], cancellationToken).ConfigureAwait(false);

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
