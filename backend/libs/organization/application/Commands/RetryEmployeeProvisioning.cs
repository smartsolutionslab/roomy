using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record RetryEmployeeProvisioning(
    WorkEmail Email,
    string InitialPassword) : ICommand;
