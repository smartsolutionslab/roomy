using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Application.Commands;

public sealed record RenameRoom(OfficeIdentifier OfficeIdentifier, RoomIdentifier RoomIdentifier, RoomName Name)
    : ICommand;
