using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class CompanyRepository(OrganizationDbContext context) : ICompanyRepository
{
    public Task<bool> ExistsAsync(CancellationToken cancellationToken) =>
        context.Companies.AnyAsync(cancellationToken);

    public async Task AddAsync(Company company, CancellationToken cancellationToken) =>
        await context.Companies.AddAsync(company, cancellationToken);

    public async Task<Result<Company>> GetSeededAsync(CancellationToken cancellationToken)
    {
        var company = await context.Companies.FirstOrDefaultAsync(cancellationToken);

        if (company is null)
            return Error.NotFound("company.not_seeded", "No company has been seeded yet.");

        return company;
    }
}
