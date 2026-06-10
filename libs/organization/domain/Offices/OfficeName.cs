using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Offices;

public sealed record OfficeName : IValueObject
{
    public string Value { get; }

    private OfficeName(string value) => Value = value;

    public static OfficeName From(string value) =>
        TryParse(value) ?? throw new ArgumentException("OfficeName must not be blank.", nameof(value));

    public static OfficeName? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new OfficeName(value.Trim());
    }

    public override string ToString() => Value;
}
