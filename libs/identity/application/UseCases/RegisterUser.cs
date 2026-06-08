using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Application.UseCases;

// The identity step of the provisioning saga (ADR-0025): provision an account for a hired employee.
// The UserIdentifier is pre-allocated upstream as the correlation key for the 1:1 User<->Employee
// link; EmployeeId is the foreign correlation carried back on the published outcome (it is another
// context's identity, so it stays a raw Guid here, not an identity value object). The initial password
// is a transient secret bound for Keycloak, never persisted.
public sealed record RegisterUser(
    UserIdentifier UserIdentifier,
    Guid EmployeeId,
    Email Email,
    DisplayName DisplayName,
    Role Role,
    string InitialPassword) : ICommand;
