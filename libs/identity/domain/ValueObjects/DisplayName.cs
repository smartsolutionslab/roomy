using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

// The user's human-readable name: required and trimmed. Equality is by value.
public sealed record DisplayName
{
    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static DisplayName Create(string value)
    {
        Ensure.That(value).IsNotNullOrWhiteSpace();

        return new DisplayName(value.Trim());
    }

    public override string ToString() => Value;
}
