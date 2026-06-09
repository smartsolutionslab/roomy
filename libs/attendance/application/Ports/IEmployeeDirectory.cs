using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Resolves the acting user (the token subject) to their EmployeeId for authorization — the 1:1
// User<->Employee link mirrored into attendance's local read model from EmployeeHired (research R3,
// ADR-0014). A user not yet known to attendance (the event has not arrived) is Error.NotFound
// (unknown_employee), never an exception.
public interface IEmployeeDirectory
{
    Task<Result<EmployeeIdentifier>> FindByUserAsync(UserIdentifier user, CancellationToken cancellationToken);
}
