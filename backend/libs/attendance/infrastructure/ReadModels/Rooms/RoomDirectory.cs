using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

public sealed class RoomDirectory(AttendanceDbContext context) : IRoomDirectory
{
    public async Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken)
    {
        var known = await context.Rooms
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.RoomId == room.Value, cancellationToken)
            .ConfigureAwait(false);

        return known is null
            ? Error.NotFound("unknown_room", "The room is not known to the attendance service yet.")
            : RoomCapacity.From(known.Capacity);
    }
}
