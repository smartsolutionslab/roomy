using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;
using Wolverine.Tracking;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging.Tests;

public sealed class WolverineIntegrationEventPublisherTests
{
    private static async Task<IHost> StartHostAsync() =>
        await Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                opts.Discovery.DisableConventionalDiscovery();
                opts.PublishMessage<TestIntegrationEvent>().ToLocalQueue("test-integration-events");
            })
            .StartAsync();

    [Fact]
    public async Task PublishAsync_publishes_the_integration_event_onto_the_bus()
    {
        using var host = await StartHostAsync();
        var publisher = new WolverineIntegrationEventPublisher(host.Services.GetRequiredService<IMessageBus>());
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid());

        var session = await host.TrackActivity().ExecuteAndWaitAsync(_ => publisher.PublishAsync(integrationEvent, CancellationToken.None));

        var published = session.Sent.SingleMessage<TestIntegrationEvent>();
        published.ShouldBe(integrationEvent);
    }

    [Fact]
    public async Task PublishAsync_throws_when_the_event_is_null()
    {
        using var host = await StartHostAsync();
        var publisher = new WolverineIntegrationEventPublisher(host.Services.GetRequiredService<IMessageBus>());

        await Should.ThrowAsync<ArgumentNullException>(() => publisher.PublishAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_throws_when_cancellation_is_already_requested()
    {
        using var host = await StartHostAsync();
        var publisher = new WolverineIntegrationEventPublisher(host.Services.GetRequiredService<IMessageBus>());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => publisher.PublishAsync(new TestIntegrationEvent(Guid.NewGuid()), cancelled.Token));
    }

    [Fact]
    public void Constructor_throws_when_the_bus_is_null()
    {
        Should.Throw<ArgumentNullException>(() => new WolverineIntegrationEventPublisher(null!));
    }
}
