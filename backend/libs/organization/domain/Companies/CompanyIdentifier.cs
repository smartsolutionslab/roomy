using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Companies;

public readonly record struct CompanyIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static CompanyIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static CompanyIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("CompanyIdentifier must not be empty.", nameof(value));

    public static CompanyIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(CompanyIdentifier identifier) => identifier.Value;

    public static implicit operator CompanyIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
