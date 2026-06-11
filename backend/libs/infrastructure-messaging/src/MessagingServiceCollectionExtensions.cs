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

public static class MessagingServiceCollectionExtensions
{
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

    public static IServiceCollection AddIntegrationEventOutbox(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventOutbox, WolverineIntegrationEventOutbox>();

        return services;
    }

    private static void ConfigureTransport(WolverineOptions wolverine, MessagingOptions options)
    {
        switch (options.Transport)
        {
            case MessagingTransport.RabbitMq:
                var connectionString = Ensure.That(options.ConnectionString).IsNotNullOrWhiteSpace().Value;
                wolverine.UseRabbitMq(new Uri(connectionString))
                    .AutoProvision()
                    .UseConventionalRouting();
                break;

            case MessagingTransport.AzureServiceBus:
            case MessagingTransport.AmazonSqs:
                throw new NotSupportedException(
                    $"Messaging transport '{options.Transport}' is a planned ADR-0015 option that is "
                    + "not wired yet. RabbitMQ is the only transport implemented today.");

            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.Transport, "Unknown messaging transport.");
        }
    }
}
