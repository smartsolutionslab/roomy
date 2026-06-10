using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record HiredEmployee(EmployeeIdentifier Employee, UserIdentifier User);
