namespace SmartSolutionsLab.Roomy.Identity.Api.Seeding;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string InitialPassword { get; init; }
}
