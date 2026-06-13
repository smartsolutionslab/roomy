using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Contracts.Organization;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;

public sealed class RoomAddedConsumer(AttendanceDbContext context)
{
    public Task Handle(RoomAdded message, CancellationToken cancellationToken) =>
        context.UpsertAsync(
            message.RoomId,
            () => new Room
            {
                RoomId = message.RoomId,
                OfficeId = message.OfficeId,
                CompanyId = message.CompanyId,
                Capacity = message.Capacity,
                Name = message.Name,
            },
            room =>
            {
                room.Capacity = message.Capacity;
                room.Name = message.Name;
            },
            cancellationToken);
}
