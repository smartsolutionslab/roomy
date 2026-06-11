using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IRoomDirectory
{
    Task<Result<RoomCapacity>> FindCapacityAsync(RoomIdentifier room, CancellationToken cancellationToken);
}
