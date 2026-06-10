using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Ports;

// Reads the occupancy data for a scope and date range from attendance's local read models (the
// Reservations projection joined to Rooms/Offices/Employees), never a cross-service join (ADR-0014/0038).
// It returns the rooms in scope and the live reservations across the range; the query handler turns
// those into figures. An office or room not known to attendance is Error.NotFound
// (unknown_office / unknown_room), not an exception.
public interface IOccupancyReadModel
{
    Task<Result<OccupancyData>> GetAsync(
        CompanyIdentifier company,
        OccupancyScope scope,
        BookingDate from,
        BookingDate to,
        CancellationToken cancellationToken);
}
