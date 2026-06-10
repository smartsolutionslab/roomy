using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands.Handlers;

public sealed class AddRoomToOfficeHandler(IOfficeRepository offices, IUnitOfWork unitOfWork)
    : ICommandHandler<AddRoomToOffice, RoomIdentifier>
{
    public async Task<Result<RoomIdentifier>> HandleAsync(AddRoomToOffice command, CancellationToken cancellationToken)
    {
        var lookup = await offices.GetByIdentifierAsync(command.OfficeIdentifier, cancellationToken);
        if (lookup.IsFailure) return lookup.Error;

        var room = lookup.Value.AddRoom(command.Name, command.Capacity);
        if (room.IsFailure) return room.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return room.Value.Identifier;
    }
}
