using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public interface IOfficeRepository
{
    Task<Result<Office>> GetByIdentifierAsync(OfficeIdentifier identifier, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        CompanyIdentifier companyIdentifier,
        OfficeName name,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Office>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(Office office, CancellationToken cancellationToken);
}
