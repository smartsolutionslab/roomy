using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public sealed record Location : IValueObject
{
    public string Value { get; }

    private Location(string value) => Value = value;

    public static Location From(string value) =>
        TryParse(value) ?? throw new ArgumentException("Location must not be blank.", nameof(value));

    public static Location? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new Location(value.Trim());
    }

    public override string ToString() => Value;
}
