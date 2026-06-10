using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class RenameRoomHandler(IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameRoom>
{
    public async Task<Result> HandleAsync(RenameRoom command, CancellationToken cancellationToken)
    {
        var (officeIdentifier, roomIdentifier, name) = command;
        var lookup = await offices.GetByIdentifierAsync(officeIdentifier, cancellationToken);
        if (lookup.IsFailure) return lookup.Error;

        var result = lookup.Value.RenameRoom(roomIdentifier, name);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
