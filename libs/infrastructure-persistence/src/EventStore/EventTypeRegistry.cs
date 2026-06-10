using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// A bidirectional, immutable map between domain event CLR types and their stable persisted names.
/// A context registers its events once at composition; the registry then resolves either direction
/// for the serializer. Names are explicit rather than derived from the CLR type so a later rename
/// or namespace move does not invalidate the existing log (ADR-0012).
/// </summary>
public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly IReadOnlyDictionary<Type, string> namesByType;
    private readonly IReadOnlyDictionary<string, Type> typesByName;

    private EventTypeRegistry(IReadOnlyDictionary<Type, string> namesByType, IReadOnlyDictionary<string, Type> typesByName)
    {
        this.namesByType = namesByType;
        this.typesByName = typesByName;
    }

    public static Builder Create() => new();

    public string GetName(Type eventType)
    {
        Ensure.That((Type?)eventType).IsNotNull();

        return namesByType.TryGetValue(eventType, out var name)
            ? name
            : throw new UnknownEventTypeException($"Event type '{eventType.FullName}' is not registered in the event-type registry.");
    }

    public Type Resolve(string eventTypeName)
    {
        Ensure.That(eventTypeName).IsNotNullOrWhiteSpace();

        return typesByName.TryGetValue(eventTypeName, out var type)
            ? type
            : throw new UnknownEventTypeException($"Event type name '{eventTypeName}' is not registered in the event-type registry.");
    }

    public sealed class Builder
    {
        private readonly Dictionary<Type, string> namesByType = [];
        private readonly Dictionary<string, Type> typesByName = new(StringComparer.Ordinal);

        /// <exception cref="ArgumentException">The type or name is already registered.</exception>
        public Builder Register<TEvent>(string persistedName)
        {
            Ensure.That(persistedName).IsNotNullOrWhiteSpace();

            var eventType = typeof(TEvent);

            if (!namesByType.TryAdd(eventType, persistedName)) throw new ArgumentException($"Event type '{eventType.FullName}' is already registered.", nameof(persistedName));

            if (!typesByName.TryAdd(persistedName, eventType))
            {
                namesByType.Remove(eventType);
                throw new ArgumentException($"Event type name '{persistedName}' is already registered.", nameof(persistedName));
            }

            return this;
        }

        public EventTypeRegistry Build() =>
            new(new Dictionary<Type, string>(namesByType), new Dictionary<string, Type>(typesByName, StringComparer.Ordinal));
    }
}
