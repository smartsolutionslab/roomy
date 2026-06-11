using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class ChangeOfficeLocationHandler(IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeOfficeLocation>
{
    public async Task<Result> HandleAsync(ChangeOfficeLocation command, CancellationToken cancellationToken)
    {
        var lookup = await offices.GetByIdentifierAsync(command.OfficeIdentifier, cancellationToken);
        if (lookup.IsFailure) return lookup.Error;

        lookup.Value.RelocateTo(command.Location);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
