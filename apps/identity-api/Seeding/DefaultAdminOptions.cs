namespace SmartSolutionsLab.Roomy.Identity.Api.Seeding;

// The DefaultAdmin credentials, bound from the "DefaultAdmin" configuration section (config/secrets,
// never hard-coded). Lets the system be administered from first start (FR-004, research R4).
public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string InitialPassword { get; init; }
}
