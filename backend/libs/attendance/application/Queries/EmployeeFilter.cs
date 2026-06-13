using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Querying;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.Attendance.Application.Queries;

public sealed record EmployeeFilter(SearchTerm Term, PageRequest Page) : Filter(Page);
