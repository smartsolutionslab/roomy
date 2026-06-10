using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

// Hire a colleague under the single seeded company (ADR-0025). The handler pre-allocates the login
// identifier and raises EmployeeHired; the initial password is a transient secret carried only on the
// event. Returns the new employee + user ids so the endpoint can answer 202 Accepted.
public sealed record HireEmployee(
    EmployeeName Name,
    WorkEmail Email,
    EmployeeRole Role,
    string InitialPassword) : ICommand<HiredEmployee>;
