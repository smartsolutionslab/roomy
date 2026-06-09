using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Companies;

// The single seeded company every office belongs to (CLAUDE.md context map). Behaviour-light in the
// MVP: it exists so office-name uniqueness has a real scope and the offices' company reference has
// referential integrity. Created once by the startup seeder.
public sealed class Company : Aggregate
{
    private Company(CompanyIdentifier identifier, CompanyName name)
    {
        Identifier = identifier;
        Name = name;
    }

    public CompanyIdentifier Identifier { get; }
    public CompanyName Name { get; private set; }

    public static Company Create(CompanyName name) => new(CompanyIdentifier.New(), name);

    public static Company Create(CompanyIdentifier identifier, CompanyName name) => new(identifier, name);
}
