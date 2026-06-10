using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record ViewOccupancy(
    CompanyIdentifier Company,
    OccupancyScope Scope,
    BookingDateRange Range) : IQuery<IReadOnlyList<OccupancyView>>;
