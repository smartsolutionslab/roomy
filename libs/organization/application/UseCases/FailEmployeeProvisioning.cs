using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.UseCases;

// Internal command raised when identity reports a provisioning failure (UserProvisioningFailed, mapped at
// the infrastructure edge). Marks the employee Failed with the reason — the compensation that prevents a
// half-account (ADR-0025, FR-007).
public sealed record FailEmployeeProvisioning(
    EmployeeIdentifier Employee,
    ProvisioningFailureReason Reason) : ICommand;
