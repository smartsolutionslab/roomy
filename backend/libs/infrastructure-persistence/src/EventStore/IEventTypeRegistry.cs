namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public interface IEventTypeRegistry
{
    string GetName(Type eventType);

    Type Resolve(string eventTypeName);
}
