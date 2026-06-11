namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public interface IEventSerializer
{
    SerializedEvent Serialize(object @event);

    object Deserialize(string eventTypeName, string payload);
}
