using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging.Tests;

/// <summary>A minimal <see cref="IIntegrationEvent"/> used to drive the publisher tests.</summary>
internal sealed record TestIntegrationEvent(Guid Id) : IIntegrationEvent;
