using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class OfficeRepository(OrganizationDbContext context) : IOfficeRepository
{
    public Task<Result<Office>> GetByIdentifierAsync(OfficeIdentifier identifier, CancellationToken cancellationToken) =>
        context.Offices.SingleOrNotFoundAsync(
            candidate => candidate.Identifier == identifier,
            Error.NotFound("office.not_found", $"No office exists with identifier '{identifier}'."),
            cancellationToken);

    public Task<bool> ExistsByNameAsync(CompanyIdentifier companyIdentifier, OfficeName name, CancellationToken cancellationToken) =>
        context.Offices.AnyAsync(office => office.CompanyIdentifier == companyIdentifier && office.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Office>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Offices.AsNoTracking().ToListAsync(cancellationToken);

    public Task AddAsync(Office office, CancellationToken cancellationToken)
    {
        context.Offices.Add(office);
        return Task.CompletedTask;
    }
}
