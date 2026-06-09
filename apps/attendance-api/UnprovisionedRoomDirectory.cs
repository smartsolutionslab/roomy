using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Api;

// Temporary IRoomDirectory until US2 wires attendance's local Rooms read model from organization's
// RoomAdded / OfficeOpened integration events. With no rooms provisioned yet every room is unknown —
// the honest US1 behaviour — so a reservation against any room is rejected as unknown_room. US2
// replaces this registration with the read-model-backed adapter.
internal sealed class UnprovisionedRoomDirectory : IRoomDirectory
{
    public Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<RoomCapacity>(
            Error.NotFound("unknown_room", "The room is not known to the attendance service yet.")));
}
