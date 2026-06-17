using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees.Events;

public sealed record EmployeeHired(
    EmployeeIdentifier Employee,
    CompanyIdentifier Company,
    UserIdentifier User,
    EmployeeName Name,
    WorkEmail Email,
    EmployeeRole Role,
    string InitialPassword) : IDomainEvent;
