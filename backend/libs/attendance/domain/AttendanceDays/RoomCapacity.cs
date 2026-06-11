using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

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
