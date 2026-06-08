namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

/// <summary>A sample event used by the serializer and event-store tests.</summary>
internal sealed record DeskBooked(Guid DeskId, string BookedBy, DateOnly OnDay);

/// <summary>A second sample event, to test appending multiple distinct event types to a stream.</summary>
internal sealed record DeskReleased(Guid DeskId, string ReleasedBy);
