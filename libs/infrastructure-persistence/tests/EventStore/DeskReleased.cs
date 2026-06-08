namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.Tests.EventStore;

internal sealed record DeskReleased(Guid DeskId, string ReleasedBy);
