using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Companies;

public interface ICompanyRepository
{
    Task<bool> ExistsAsync(CancellationToken cancellationToken);

    Task AddAsync(Company company, CancellationToken cancellationToken);

    Task<Result<Company>> GetSeededAsync(CancellationToken cancellationToken);
}
