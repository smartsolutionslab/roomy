using System.Reflection;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Model;
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
    /// <param name="applicationAssembly">
    /// The host assembly Wolverine treats as the application — the assembly whose committed
    /// <c>Internal/Generated</c> code is loaded under <see cref="TypeLoadMode.Static"/> (ADR-0034). It
    /// must be the host the static code was generated for, not this shared messaging library (which is
    /// where <c>UseWolverine</c> is invoked, and what Wolverine would otherwise default to). Pass a
    /// stable marker type's assembly (e.g. <c>typeof(IdentityApiHost).Assembly</c>). Optional — when
    /// omitted (e.g. a test that never starts the runtime), Wolverine keeps its default.
    /// </param>
    /// <param name="handlerAssemblies">
    /// Assemblies Wolverine should scan for message handlers/consumers (e.g. a context's
    /// infrastructure assembly that consumes another context's integration events). Wolverine always
    /// scans the application assembly; a context's consumers live in its own assembly, so they are
    /// included here. Optional — pass none for a publish-only host.
    /// </param>
    public static IHostApplicationBuilder AddRoomyMessaging(
        this IHostApplicationBuilder builder,
        MessagingOptions options,
        Assembly? applicationAssembly = null,
        params Assembly[] handlerAssemblies)
    {
        Ensure.That((IHostApplicationBuilder?)builder).IsNotNull();
        Ensure.That((MessagingOptions?)options).IsNotNull();
        var postgresConnectionString = Ensure.That(options.PostgresConnectionString).IsNotNullOrWhiteSpace().Value;

        builder.UseWolverine(wolverine =>
        {
            // Wolverine defaults the application assembly to the caller of UseWolverine — this shared
            // library — but the static-codegen output is committed in the host. Pin it to the host so
            // the pre-generated HandlerRegistry is found at runtime rather than triggering a fallback
            // scan (or, worse, runtime compilation that is no longer available — see below).
            if (applicationAssembly is not null)
            {
                wolverine.ApplicationAssembly = applicationAssembly;
            }

            // Load pre-generated handler/middleware types instead of compiling them at runtime
            // (ADR-0034). WolverineFx 6.5 no longer ships the Roslyn runtime compiler in core (GH-2876),
            // so a host MUST either carry the compiler or run from code generated ahead of time. We take
            // the latter: each host commits the output of `dotnet run -- codegen write` and loads it
            // statically — no Roslyn at runtime (smaller image, faster cold start, AOT-friendly).
            wolverine.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;

            // Let the generated handler code resolve dependencies it cannot construct inline — notably
            // ports registered as typed HttpClients (an opaque factory, e.g. the Keycloak-backed
            // IIdentityProviderPort) — from the scoped IServiceProvider. This is plain service location,
            // not runtime compilation, so it keeps the static-codegen guarantee (no Roslyn at runtime)
            // while staying generic: the shared messaging composition stays free of any context's types.
            wolverine.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

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
        builder.Services.AddScoped<IIntegrationEventOutbox, WolverineIntegrationEventOutbox>();

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
