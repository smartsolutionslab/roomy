using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The number of places a room offers on a day — the ceiling the no-overbooking rule enforces
// (FR-004/FR-007). At least one place; capacity is organization's master data, handed to the
// aggregate at reservation time (research R3), never stored on it.
public readonly record struct RoomCapacity : IValueObject
{
    public int Value { get; private init; }

    public static RoomCapacity From(int value) =>
        TryParse(value) ?? throw new ArgumentException("RoomCapacity must be at least one place.", nameof(value));

    public static RoomCapacity? TryParse(int value)
    {
        if (value < 1) return null;
        return new() { Value = value };
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
