using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public readonly record struct RoomIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static RoomIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static RoomIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("RoomIdentifier must not be empty.", nameof(value));

    public static RoomIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(RoomIdentifier identifier) => identifier.Value;

    public static implicit operator RoomIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
