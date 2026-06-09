using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// A room's name: required and trimmed. Equality is by value; uniqueness within the office is enforced
// by the Office aggregate (and a unique index).
public sealed record RoomName : IValueObject
{
    public string Value { get; }

    private RoomName(string value) => Value = value;

    public static RoomName From(string value) =>
        TryParse(value) ?? throw new ArgumentException("RoomName must not be blank.", nameof(value));

    public static RoomName? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new RoomName(value.Trim());
    }

    public override string ToString() => Value;
}
