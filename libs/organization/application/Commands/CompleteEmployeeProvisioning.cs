using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

// Internal command raised when identity acknowledges a provisioned account (UserRegistered, mapped at
// the infrastructure edge). Marks the employee Active (ADR-0025).
public sealed record CompleteEmployeeProvisioning(EmployeeIdentifier Employee) : ICommand;
