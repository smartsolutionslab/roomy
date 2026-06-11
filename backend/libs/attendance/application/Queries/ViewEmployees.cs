using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record ViewEmployees(SearchTerm Term, PageRequest Page) : IQuery<Page<EmployeeView>>;
