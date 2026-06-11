using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging.Tests;

internal sealed record TestIntegrationEvent(Guid Id) : IIntegrationEvent;
