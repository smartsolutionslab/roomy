using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices.Events;
using SmartSolutionsLab.Roomy.SharedKernel;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public sealed class Office : Aggregate
{
    private readonly List<Room> rooms = [];

    private Office(
        OfficeIdentifier identifier,
        CompanyIdentifier companyIdentifier,
        OfficeName name,
        Location location)
    {
        Identifier = identifier;
        CompanyIdentifier = companyIdentifier;
        Name = name;
        Location = location;
    }

    public OfficeIdentifier Identifier { get; }
    public CompanyIdentifier CompanyIdentifier { get; }
    public OfficeName Name { get; private set; }
    public Location Location { get; private set; }
    public IReadOnlyList<Room> Rooms => rooms;
    public int Capacity => rooms.Sum(room => room.Capacity.Value);

    public static Office Create(CompanyIdentifier companyIdentifier, OfficeName name, Location location)
    {
        var office = new Office(OfficeIdentifier.New(), companyIdentifier, name, location);
        office.RaiseDomainEvent(new OfficeOpened(office.Identifier, companyIdentifier, name, location));
        return office;
    }

    public void Rename(OfficeName name) => Name = name;

    public void RelocateTo(Location location) => Location = location;

    public Result<Room> AddRoom(RoomName name, Capacity capacity)
    {
        if (rooms.Any(room => room.Name == name))
        {
            return Error.Conflict("office.room_name_taken", $"A room named '{name}' already exists in this office.");
        }

        var room = Room.Create(name, capacity);
        rooms.Add(room);
        RaiseDomainEvent(new RoomAdded(room.Identifier, Identifier, CompanyIdentifier, name, capacity));
        return room;
    }

    public Result RenameRoom(RoomIdentifier roomIdentifier, RoomName name)
    {
        var room = rooms.SingleOrDefault(candidate => candidate.Identifier == roomIdentifier);
        if (room is null) return Error.NotFound("office.room_not_found", $"No room with identifier '{roomIdentifier}' exists in this office.");

        if (IsRoomNameTakenByAnother(roomIdentifier, name))
        {
            return Error.Conflict("office.room_name_taken", $"A room named '{name}' already exists in this office.");
        }

        room.Rename(name);
        return Result.Success();
    }

    private bool IsRoomNameTakenByAnother(RoomIdentifier roomIdentifier, RoomName name) =>
        rooms.Any(candidate => candidate.Identifier != roomIdentifier && candidate.Name == name);
}
