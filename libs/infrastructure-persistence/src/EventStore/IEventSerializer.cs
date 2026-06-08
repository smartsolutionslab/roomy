namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Serializes a domain event to the persisted name + payload pair stored in the events table, and
/// reconstructs the event from them on read. Implementations own the wire format (JSON for v1,
/// ADR-0012) and delegate the name &lt;-&gt; type mapping to an <see cref="IEventTypeRegistry"/>.
/// </summary>
public interface IEventSerializer
{
    /// <summary>Serializes <paramref name="event"/> to its stable type name and payload string.</summary>
    SerializedEvent Serialize(object @event);

    /// <summary>Reconstructs an event instance from its persisted type name and payload.</summary>
    object Deserialize(string eventTypeName, string payload);
}

/// <summary>The persisted form of an event: its stable type name and serialized payload.</summary>
public readonly record struct SerializedEvent(string TypeName, string Payload);
