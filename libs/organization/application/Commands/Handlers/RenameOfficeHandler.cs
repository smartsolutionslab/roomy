using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class RenameOfficeHandler(IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameOffice>
{
    public async Task<Result> HandleAsync(RenameOffice command, CancellationToken cancellationToken)
    {
        var (officeIdentifier, name) = command;
        var lookup = await offices.GetByIdentifierAsync(officeIdentifier, cancellationToken);
        if (lookup.IsFailure) return lookup.Error;

        var office = lookup.Value;

        // A no-op rename to the same name is allowed; a clash with another office in the company is not.
        if (command.Name != office.Name && await offices.ExistsByNameAsync(office.CompanyIdentifier, name, cancellationToken))
        {
            return Error.Conflict("office.name_taken", $"An office named '{name}' already exists.");
        }

        office.Rename(command.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
