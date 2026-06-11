using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Companies;

public sealed record CompanyName : IValueObject
{
    public string Value { get; }

    private CompanyName(string value) => Value = value;

    public static CompanyName From(string value) =>
        TryParse(value) ?? throw new ArgumentException("CompanyName must not be blank.", nameof(value));

    public static CompanyName? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new CompanyName(value.Trim());
    }

    public override string ToString() => Value;
}
