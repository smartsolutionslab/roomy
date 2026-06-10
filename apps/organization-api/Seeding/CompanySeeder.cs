using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

// Seeds the single company at startup so offices have a company to belong to (research.md D2).
// Idempotent: once a company exists, a restart is a no-op.
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
