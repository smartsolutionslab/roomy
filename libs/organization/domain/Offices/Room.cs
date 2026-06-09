using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// A room inside an Office. An entity of the Office aggregate — created and renamed only through the
// Office, never on its own, so the office can enforce room-name uniqueness and the derived capacity.
public sealed class Room : IEntity
{
    private Room(RoomIdentifier identifier, RoomName name, Capacity capacity)
    {
        Identifier = identifier;
        Name = name;
        Capacity = capacity;
    }

    public RoomIdentifier Identifier { get; }
    public RoomName Name { get; private set; }
    public Capacity Capacity { get; }

    internal static Room Create(RoomName name, Capacity capacity) =>
        new(RoomIdentifier.New(), name, capacity);

    internal void Rename(RoomName name) => Name = name;
}
