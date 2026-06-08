namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// Every account holds the Employee role; Administrator is an elevation, never a standalone role
// (FR-001/FR-002, research R5). The identity aggregate is the source of truth for the assignment.
public readonly record struct Role
{
    public bool IsAdministrator { get; private init; }

    // The base role every account carries.
    public static Role Employee => new() { IsAdministrator = false };

    // Elevate to administrator. Idempotent — granting an already-administrator role is a no-op.
    public Role GrantAdministrator() => this with { IsAdministrator = true };
}
