using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Lists the company's bookable rooms from attendance's own Offices/Rooms read models (007 US1), joined
// for their names — never a cross-service join (ADR-0014). A company with no offices/rooms yet yields an
// empty list; absence is not an error here.
public interface IBookableRoomsReadModel
{
    Task<IReadOnlyList<BookableRoomView>> GetAsync(CompanyIdentifier company, CancellationToken cancellationToken);
}
