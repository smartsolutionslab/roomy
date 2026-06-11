using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public readonly record struct StreamId
{
    private StreamId(Guid value) => Value = value;

    public Guid Value { get; }

    public static StreamId From(Guid value)
    {
        Ensure.That(value).Satisfies(static id => id != Guid.Empty, "Stream id must not be empty.");
        return new StreamId(value);
    }

    public override string ToString() => Value.ToString();
}
