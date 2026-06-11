using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record ViewMyReservations(EmployeeIdentifier Employee, PageRequest Page)
    : IQuery<Page<MyReservationView>>;
