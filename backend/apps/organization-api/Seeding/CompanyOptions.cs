using System.ComponentModel.DataAnnotations;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

public sealed class CompanyOptions
{
    public const string SectionName = "Company";

    [Required]
    public required string Name { get; init; }
}
