using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// The per-stream version: the count of events already in a stream, and therefore the expected
/// version a writer asserts when appending (optimistic concurrency). A fresh, never-written stream
/// is at <see cref="None"/> (0); each appended event advances the version by one. The database
/// enforces the contract with a unique constraint on <c>(stream_id, version)</c> (ADR-0012); this
/// value object makes the version a first-class concept rather than a bare <see cref="int"/>.
/// </summary>
public readonly record struct StreamVersion
{
    private StreamVersion(int value) => Value = value;

    /// <summary>The version of a stream that has no events yet.</summary>
    public static StreamVersion None => new(0);

    /// <summary>The non-negative version number.</summary>
    public int Value { get; }

    /// <summary>Creates a version from a non-negative number.</summary>
    public static StreamVersion From(int value)
    {
        Ensure.That(value).Satisfies(static v => v >= 0, "Stream version must not be negative.");
        return new StreamVersion(value);
    }

    /// <summary>The version after one more event is appended.</summary>
    public StreamVersion Next() => new(Value + 1);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
