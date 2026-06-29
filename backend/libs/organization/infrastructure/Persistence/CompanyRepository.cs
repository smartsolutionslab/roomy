using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class CompanyRepository(OrganizationDbContext context) : ICompanyRepository
{
    public Task<bool> ExistsAsync(CancellationToken cancellationToken) =>
        context.Companies.AnyAsync(cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        context.Companies.Add(company);
        return Task.CompletedTask;
    }

    public async Task<Result<Company>> GetSeededAsync(CancellationToken cancellationToken)
    {
        var company = await context.Companies.FirstOrDefaultAsync(cancellationToken);

        if (company is null) return Error.NotFound("company.not_seeded", "No company has been seeded yet.");

        return company;
    }
}
