using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

// Add a room to an existing office. Returns the new room's identifier.
public sealed record AddRoomToOffice(
    OfficeIdentifier OfficeIdentifier,
    RoomName Name, Capacity Capacity)
    : ICommand<RoomIdentifier>;
