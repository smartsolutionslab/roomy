using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

public sealed record EmployeeName : IValueObject
{
    public string Value { get; }

    private EmployeeName(string value) => Value = value;

    public static EmployeeName From(string value) =>
        TryParse(value) ?? throw new ArgumentException("EmployeeName must not be blank.", nameof(value));

    public static EmployeeName? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new EmployeeName(value.Trim());
    }

    public override string ToString() => Value;
}
