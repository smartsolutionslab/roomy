using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Companies;

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
