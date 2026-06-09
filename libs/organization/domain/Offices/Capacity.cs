using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// A room's capacity: a positive whole number of places, at least 1 (FR-007), fixed at creation in the
// MVP. The implicit int conversion keeps the EF Core value converter trivial.
public readonly record struct Capacity : IValueObject
{
    public int Value { get; private init; }

    public static Capacity From(int value) =>
        TryParse(value) ?? throw new ArgumentException("Capacity must be at least 1.", nameof(value));

    public static Capacity? TryParse(int value)
    {
        if (value < 1) return null;
        return new() { Value = value };
    }

    public static implicit operator int(Capacity capacity) => capacity.Value;

    public override string ToString() => Value.ToString();
}
