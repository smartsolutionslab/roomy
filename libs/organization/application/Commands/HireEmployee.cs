using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record HireEmployee(
    EmployeeName Name,
    WorkEmail Email,
    EmployeeRole Role,
    string InitialPassword) : ICommand<HiredEmployee>;
