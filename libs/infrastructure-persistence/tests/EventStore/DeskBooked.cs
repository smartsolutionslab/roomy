namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

internal sealed record DeskBooked(Guid DeskId, string BookedBy, DateOnly OnDay);
