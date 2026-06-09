using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.UseCases;

public sealed class CreateOfficeHandler(
    ICompanyRepository companies,
    IOfficeRepository offices,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateOffice, OfficeIdentifier>
{
    public async Task<Result<OfficeIdentifier>> HandleAsync(
        CreateOffice command,
        CancellationToken cancellationToken)
    {
        var company = await companies.GetSeededAsync(cancellationToken);
        if (company.IsFailure)
            return company.Error;

        if (await offices.ExistsByNameAsync(company.Value.Identifier, command.Name, cancellationToken))
            return Error.Conflict("office.name_taken", $"An office named '{command.Name}' already exists.");

        var office = Office.Create(company.Value.Identifier, command.Name, command.Location);
        await offices.AddAsync(office, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return office.Identifier;
    }
}
