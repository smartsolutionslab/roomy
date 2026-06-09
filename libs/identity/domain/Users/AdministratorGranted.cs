using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// Raised when an account is elevated to Administrator (US4 / IA-4, data-model.md). An intra-context
// domain event with no cross-context consumer in the MVP (ADR-0032): it records that the elevation
// happened. OccurredAt is supplied by the caller's clock, never read ambiently.
public sealed record AdministratorGranted(UserIdentifier UserId, DateTimeOffset OccurredAt) : IDomainEvent;
