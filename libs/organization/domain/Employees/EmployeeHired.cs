using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// Raised when an employee is hired (ADR-0025/0032). Intra-context and framework-free — it carries the
// employee's value objects and the **transient initial password**, which the infrastructure unit of work
// drains at commit and maps onto the EmployeeHired integration contract so identity can set the credential
// (ADR-0037). The password rides this event only; it is never persisted on the employee (FR-009).
public sealed record EmployeeHired(
    EmployeeIdentifier Employee,
    CompanyIdentifier Company,
    UserIdentifier User,
    EmployeeName Name,
    WorkEmail Email,
    EmployeeRole Role,
    string InitialPassword) : IDomainEvent;
