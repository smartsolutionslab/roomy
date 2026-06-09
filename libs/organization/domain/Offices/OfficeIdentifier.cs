using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

// The identity of an Office: a branded, time-ordered GUIDv7. The implicit Guid conversions keep the
// EF Core value converter trivial.
public readonly record struct OfficeIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static OfficeIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static OfficeIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("OfficeIdentifier must not be empty.", nameof(value));

    public static OfficeIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(OfficeIdentifier identifier) => identifier.Value;

    public static implicit operator OfficeIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
