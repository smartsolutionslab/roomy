using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Supplies a room's capacity — organization's master data, mirrored into attendance's local read
// model via integration events (research R3, ADR-0014). The reserve use case reads capacity here and
// hands it to the aggregate, so the domain never reaches across the context boundary. A room not yet
// known to attendance is Error.NotFound (unknown_room), not an exception.
public interface IRoomDirectory
{
    Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken);
}
