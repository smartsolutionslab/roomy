using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// The identity of an event stream — the aggregate instance whose events the stream records. A
/// non-empty <see cref="Guid"/> wrapped as a value object so a stream is never addressed by a bare
/// primitive (ADR-0012; coding standards: no primitive obsession).
/// </summary>
public readonly record struct StreamId
{
    private StreamId(Guid value) => Value = value;

    /// <summary>The underlying non-empty identifier.</summary>
    public Guid Value { get; }

    /// <summary>Creates a stream id from a non-empty <see cref="Guid"/>.</summary>
    public static StreamId From(Guid value)
    {
        Ensure.That(value).Satisfies(static id => id != Guid.Empty, "Stream id must not be empty.");
        return new StreamId(value);
    }

    public override string ToString() => Value.ToString();
}
