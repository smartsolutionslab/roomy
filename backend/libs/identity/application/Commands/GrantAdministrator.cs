using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Application.Commands;

public sealed record GrantAdministrator(UserIdentifier UserId) : ICommand;
