using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IBookableRoomsReadModel
{
    Task<IReadOnlyList<BookableRoomView>> GetAsync(CompanyIdentifier company, CancellationToken cancellationToken);
}
