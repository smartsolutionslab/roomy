using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Application.Commands;

public sealed record RegisterUser(
    UserIdentifier UserIdentifier,
    Guid EmployeeId,
    Email Email,
    DisplayName DisplayName,
    Role Role,
    string InitialPassword) : ICommand;
