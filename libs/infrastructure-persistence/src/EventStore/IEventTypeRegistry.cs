namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>
/// Maps domain event CLR types to and from the stable, persisted type name stored alongside each
/// event. The persisted name is decoupled from the CLR type name so events can be renamed or moved
/// across namespaces without breaking the log — the correctness-critical piece ADR-0012 calls out
/// as "owned": event versioning/upcasting builds on this seam.
/// </summary>
public interface IEventTypeRegistry
{
    /// <summary>Resolves the stable persisted name for a CLR event type.</summary>
    /// <exception cref="UnknownEventTypeException">The type is not registered.</exception>
    string GetName(Type eventType);

    /// <summary>Resolves the CLR event type for a persisted name.</summary>
    /// <exception cref="UnknownEventTypeException">The name is not registered.</exception>
    Type Resolve(string eventTypeName);
}
