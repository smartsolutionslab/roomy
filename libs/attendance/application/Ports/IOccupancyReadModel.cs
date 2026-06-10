using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

public interface IOccupancyReadModel
{
    Task<Result<OccupancyData>> GetAsync(
        CompanyIdentifier company,
        OccupancyScope scope,
        BookingDateRange range,
        CancellationToken cancellationToken);
}
