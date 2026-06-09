using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The room a place is reserved in — the unit the no-overbooking rule counts against (per room, day).
// References organization's Room by id only (ADR-0014): a branded GUIDv7. The implicit conversions
// keep the EF Core value converter trivial.
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
