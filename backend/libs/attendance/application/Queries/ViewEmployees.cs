using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record ViewEmployees(EmployeeFilter Filter) : IQuery<Page<EmployeeView>>;
