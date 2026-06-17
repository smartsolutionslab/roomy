using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class CreateOfficeHandler(ICompanyRepository companies, IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOffice, OfficeIdentifier>
{
    public async Task<Result<OfficeIdentifier>> HandleAsync(CreateOffice command, CancellationToken cancellationToken)
    {
        var (name, location) = command;
        var company = await companies.GetSeededAsync(cancellationToken);
        if (company.IsFailure) return company.Error;

        if (await offices.ExistsByNameAsync(company.Value.Identifier, name, cancellationToken))
        {
            return OfficeErrors.NameTaken(name);
        }

        var office = Office.Create(company.Value.Identifier, name, location);
        await offices.AddAsync(office, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return office.Identifier;
    }
}
