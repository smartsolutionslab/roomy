using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging.Tests;

/// <summary>
/// Verifies the composition-root wiring (ADR-0005, ADR-0015): the owned
/// <see cref="IIntegrationEventPublisher"/> port is bound to its Wolverine-backed implementation and
/// the transport-selection seam behaves per ADR-0015 — RabbitMQ wires, the planned transports throw
/// until they are implemented. Wolverine's configure runs eagerly during <c>UseWolverine</c>, so the
/// seam is exercised here without starting the host or connecting to a broker (which the Docker-less
/// CI cannot do; a live round-trip is deferred to #68).
/// </summary>
public sealed class MessagingServiceCollectionExtensionsTests
{
    private const string PostgresConnectionString = "Host=localhost;Database=roomy;Username=roomy;Password=roomy";
    private const string RabbitConnectionString = "amqp://guest:guest@localhost:5672";

    private static MessagingOptions RabbitOptions() => new()
    {
        Transport = MessagingTransport.RabbitMq,
        ConnectionString = RabbitConnectionString,
        PostgresConnectionString = PostgresConnectionString,
    };

    [Fact]
    public void AddRoomyMessaging_binds_the_integration_event_publisher_port_to_the_wolverine_adapter()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddRoomyMessaging(RabbitOptions());

        var registration = Assert.Single(
            builder.Services,
            descriptor => descriptor.ServiceType == typeof(IIntegrationEventPublisher));
        Assert.Equal(typeof(WolverineIntegrationEventPublisher), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddRoomyMessaging_wires_the_default_rabbitmq_transport_without_a_live_broker()
    {
        var builder = Host.CreateApplicationBuilder();

        var returned = builder.AddRoomyMessaging(RabbitOptions());

        Assert.Same(builder, returned);
    }

    [Theory]
    [InlineData(MessagingTransport.AzureServiceBus)]
    [InlineData(MessagingTransport.AmazonSqs)]
    public void AddRoomyMessaging_rejects_a_transport_that_is_not_wired_yet(MessagingTransport transport)
    {
        var builder = Host.CreateApplicationBuilder();
        var options = RabbitOptions();
        options.Transport = transport;

        Assert.Throws<NotSupportedException>(() => builder.AddRoomyMessaging(options));
    }

    [Fact]
    public void AddRoomyMessaging_requires_a_rabbitmq_connection_string_for_the_rabbitmq_transport()
    {
        var builder = Host.CreateApplicationBuilder();
        var options = RabbitOptions();
        options.ConnectionString = null;

        Assert.ThrowsAny<ArgumentException>(() => builder.AddRoomyMessaging(options));
    }

    [Fact]
    public void AddRoomyMessaging_requires_a_postgres_connection_string_for_the_durable_outbox()
    {
        var builder = Host.CreateApplicationBuilder();
        var options = RabbitOptions();
        options.PostgresConnectionString = null;

        Assert.ThrowsAny<ArgumentException>(() => builder.AddRoomyMessaging(options));
    }

    [Fact]
    public void AddRoomyMessaging_rejects_a_null_builder()
    {
        Assert.Throws<ArgumentNullException>(
            () => MessagingServiceCollectionExtensions.AddRoomyMessaging(null!, RabbitOptions()));
    }

    [Fact]
    public void AddRoomyMessaging_rejects_null_options()
    {
        var builder = Host.CreateApplicationBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.AddRoomyMessaging(null!));
    }

    [Fact]
    public void MessagingOptions_default_transport_is_rabbitmq()
    {
        Assert.Equal(MessagingTransport.RabbitMq, new MessagingOptions().Transport);
    }
}
