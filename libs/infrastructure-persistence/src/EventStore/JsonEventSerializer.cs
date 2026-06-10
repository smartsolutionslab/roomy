using System.Text.Json;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public sealed class JsonEventSerializer(IEventTypeRegistry typeRegistry, JsonSerializerOptions? serializerOptions = null)
    : IEventSerializer
{
    private readonly JsonSerializerOptions serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public SerializedEvent Serialize(object @event)
    {
        var typeName = typeRegistry.GetName(@event.GetType());
        var payload = JsonSerializer.Serialize(@event, @event.GetType(), serializerOptions);

        return new SerializedEvent(typeName, payload);
    }

    public object Deserialize(string eventTypeName, string payload)
    {
        Ensure.That(eventTypeName).IsNotNullOrWhiteSpace();

        var eventType = typeRegistry.Resolve(eventTypeName);

        return JsonSerializer.Deserialize(payload, eventType, serializerOptions)
            ?? throw new EventDeserializationException($"Payload for event type '{eventTypeName}' deserialized to null.");
    }
}
