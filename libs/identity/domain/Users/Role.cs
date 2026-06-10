using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

// Every account holds the Employee role; Administrator is an elevation, never a standalone role
// (FR-001/FR-002, research R5). The identity aggregate is the source of truth for the assignment.
public readonly record struct Role : IValueObject
{
    public bool IsAdministrator { get; private init; }

    public static Role Employee => new() { IsAdministrator = false };

    // Idempotent — granting an already-administrator role is a no-op.
    public Role GrantAdministrator() => this with { IsAdministrator = true };
}
