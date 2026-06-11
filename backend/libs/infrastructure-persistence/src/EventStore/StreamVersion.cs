using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public readonly record struct StreamVersion
{
    private StreamVersion(int value) => Value = value;

    public static StreamVersion None => new(0);

    public int Value { get; }

    public static StreamVersion From(int value)
    {
        Ensure.That(value).Satisfies(static version => version >= 0, "Stream version must not be negative.");
        return new StreamVersion(value);
    }

    public StreamVersion Next() => new(Value + 1);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
