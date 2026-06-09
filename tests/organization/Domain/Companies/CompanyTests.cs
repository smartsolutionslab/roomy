using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.Companies;

public sealed class CompanyTests
{
    [Fact]
    public void Create_assigns_the_name_and_a_new_identifier()
    {
        var company = Company.Create(CompanyName.From("Acme"));

        company.Name.Value.ShouldBe("Acme");
        company.Identifier.Value.ShouldNotBe(Guid.Empty);
    }
}
