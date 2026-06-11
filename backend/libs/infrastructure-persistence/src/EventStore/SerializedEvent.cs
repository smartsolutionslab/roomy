namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

public readonly record struct SerializedEvent(string TypeName, string Payload);
