using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

// The result of hiring: the new employee and the pre-allocated login identifier the saga correlates on
// (ADR-0025). The endpoint answers 202 with both, plus the Provisioning state.
public sealed record HiredEmployee(EmployeeIdentifier Employee, UserIdentifier User);
