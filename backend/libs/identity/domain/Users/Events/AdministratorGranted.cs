using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users.Events;

public sealed record AdministratorGranted(UserIdentifier UserId, DateTimeOffset OccurredAt) : IDomainEvent;
