using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public readonly record struct Role : IValueObject
{
    public bool IsAdministrator { get; private init; }

    public static Role Employee => new() { IsAdministrator = false };

    public Role GrantAdministrator() => this with { IsAdministrator = true };
}
