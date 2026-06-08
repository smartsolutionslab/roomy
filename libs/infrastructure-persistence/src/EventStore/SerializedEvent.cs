namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

/// <summary>The persisted form of an event: its stable type name and serialized payload.</summary>
public readonly record struct SerializedEvent(string TypeName, string Payload);
