using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

// Rooms are an owned collection, so EF loads them with their office automatically — no explicit
// Include is needed.
public sealed class OfficeRepository(OrganizationDbContext context) : IOfficeRepository
{
    public async Task<Result<Office>> GetByIdentifierAsync(OfficeIdentifier identifier, CancellationToken cancellationToken)
    {
        var office = await context.Offices.SingleOrDefaultAsync(candidate => candidate.Identifier == identifier, cancellationToken);

        if (office is null) return Error.NotFound("office.not_found", $"No office exists with identifier '{identifier}'.");

        return office;
    }

    public Task<bool> ExistsByNameAsync(CompanyIdentifier companyIdentifier, OfficeName name, CancellationToken cancellationToken) =>
        context.Offices.AnyAsync(office => office.CompanyIdentifier == companyIdentifier && office.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Office>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Offices.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Office office, CancellationToken cancellationToken) =>
        await context.Offices.AddAsync(office, cancellationToken);
}
