using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

public sealed record EncryptedCredential : IValueObject
{
    public string Value { get; }

    private EncryptedCredential(string value) => Value = value;

    public static EncryptedCredential From(string value) =>
        TryParse(value) ?? throw new ArgumentException("EncryptedCredential must not be blank.", nameof(value));

    public static EncryptedCredential? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new EncryptedCredential(value);
    }

    public override string ToString() => "[encrypted credential]";
}
