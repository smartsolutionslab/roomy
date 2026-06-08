using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace SmartSolutionsLab.Roomy.Infrastructure.Messaging;

/// <summary>
/// Composition-root wiring for the messaging backbone (ADR-0005, ADR-0014, ADR-0015). This is the
/// <em>only</em> place Wolverine is referenced: it configures Wolverine's durable transactional
/// outbox/inbox over EF Core + PostgreSQL (sharing the context's transaction so a publish commits
/// atomically with the aggregate write, ADR-0012:76), selects the transport from configuration
/// (RabbitMQ default), and binds the owned <see cref="IIntegrationEventPublisher"/> port to its
/// Wolverine-backed implementation. <c>domain</c> and <c>application</c> never see any of this.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Wolverine-backed messaging backbone to the host. Registers the owned
    /// <see cref="IIntegrationEventPublisher"/> port over Wolverine and configures the durable
    /// outbox/inbox plus the selected transport.
    /// </summary>
    /// <param name="builder">The host application builder (composition root).</param>
    /// <param name="options">The resolved messaging options (transport + connection strings).</param>
    /// <param name="handlerAssemblies">
    /// Assemblies Wolverine should scan for message handlers/consumers (e.g. a context's
    /// infrastructure assembly that consumes another context's integration events). Wolverine always
    /// scans the entry assembly; a context's consumers live in its own assembly, so they are included
    /// here. Optional — pass none for a publish-only host.
    /// </param>
    public static IHostApplicationBuilder AddRoomyMessaging(
        this IHostApplicationBuilder builder,
        MessagingOptions options,
        params Assembly[] handlerAssemblies)
    {
        Ensure.That((IHostApplicationBuilder?)builder).IsNotNull();
        Ensure.That((MessagingOptions?)options).IsNotNull();
        var postgresConnectionString = Ensure.That(options.PostgresConnectionString).IsNotNullOrWhiteSpace().Value;

        builder.UseWolverine(wolverine =>
        {
            // Durable transactional outbox/inbox on the same PostgreSQL as the context's
            // RoomyDbContext: the publish is captured in the outbox within the EF transaction, so it
            // commits atomically with the aggregate write and is relayed at-least-once thereafter
            // (ADR-0012:76). The inbox gives idempotent, dedup'd delivery (ADR-0015).
            wolverine.PersistMessagesWithPostgresql(postgresConnectionString);
            wolverine.UseEntityFrameworkCoreTransactions();
            wolverine.Policies.AutoApplyTransactions();
            wolverine.Policies.UseDurableOutboxOnAllSendingEndpoints();
            wolverine.Policies.UseDurableInboxOnAllListeners();

            foreach (var handlerAssembly in handlerAssemblies)
            {
                wolverine.Discovery.IncludeAssembly(handlerAssembly);
            }

            ConfigureTransport(wolverine, options);
        });

        builder.Services.AddScoped<IIntegrationEventPublisher, WolverineIntegrationEventPublisher>();

        return builder;
    }

    /// <summary>
    /// The transport-selection seam (ADR-0015). RabbitMQ is the only transport wired today; the
    /// switch point exists so Azure Service Bus / Amazon SQS+SNS slot in here without touching the
    /// publish path or the core. Adding a transport is a change to this method and config only.
    /// </summary>
    private static void ConfigureTransport(WolverineOptions wolverine, MessagingOptions options)
    {
        switch (options.Transport)
        {
            case MessagingTransport.RabbitMq:
                var connectionString = Ensure.That(options.ConnectionString).IsNotNullOrWhiteSpace().Value;
                wolverine.UseRabbitMq(new Uri(connectionString))
                    .AutoProvision();
                break;

            case MessagingTransport.AzureServiceBus:
            case MessagingTransport.AmazonSqs:
                throw new NotSupportedException(
                    $"Messaging transport '{options.Transport}' is a planned ADR-0015 option that is "
                    + "not wired yet. RabbitMQ is the only transport implemented today.");

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.Transport,
                    "Unknown messaging transport.");
        }
    }
}
