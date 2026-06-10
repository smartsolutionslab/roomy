using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Application.Commands;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class RenameRoomHandler(IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameRoom>
{
    public async Task<Result> HandleAsync(RenameRoom command, CancellationToken cancellationToken)
    {
        var lookup = await offices.GetByIdentifierAsync(command.OfficeIdentifier, cancellationToken);
        if (lookup.IsFailure)
            return lookup.Error;

        var result = lookup.Value.RenameRoom(command.RoomIdentifier, command.Name);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
