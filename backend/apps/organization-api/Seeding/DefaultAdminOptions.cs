using System.ComponentModel.DataAnnotations;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

public sealed class DefaultAdminOptions
{
    public const string SectionName = "DefaultAdmin";

    [Required]
    public required string Email { get; init; }

    [Required]
    public required string DisplayName { get; init; }

    [Required]
    public required string InitialPassword { get; init; }
}
