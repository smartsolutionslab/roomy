namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

// Bound from the `Company` configuration section. The MVP has a single company, seeded at startup so
// offices have a real company to belong to (research.md D2).
public sealed class CompanyOptions
{
    public const string SectionName = "Company";

    public required string Name { get; init; }
}
