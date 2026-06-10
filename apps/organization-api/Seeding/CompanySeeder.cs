using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

public sealed class CompanySeeder(ICompanyRepository companies, OrganizationDbContext dbContext, CompanyOptions options)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await companies.ExistsAsync(cancellationToken)) return;

        var company = Company.Create(CompanyName.From(options.Name));
        await companies.AddAsync(company, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
