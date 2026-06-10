using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

// Intent to view occupancy for a scope (an office or a room) over a date range (FR-001/002, scenarios
// 1–3). Any authenticated user may view any office or room (FR-005), so there is no actor here. The date
// range is inclusive and may include past days (FR-009); the endpoint defaults and bounds it.
public sealed record ViewOccupancy(
    CompanyIdentifier Company,
    OccupancyScope Scope,
    BookingDate From,
    BookingDate To) : IQuery<IReadOnlyList<OccupancyView>>;
