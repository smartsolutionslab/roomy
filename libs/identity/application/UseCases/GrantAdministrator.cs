using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Application.UseCases;

// Elevates an existing account to Administrator (US4 / IA-4). The admin REST surface dispatches it when
// an administrator grants the role to an account identified by its UserIdentifier.
public sealed record GrantAdministrator(UserIdentifier UserId) : ICommand;
