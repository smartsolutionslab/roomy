using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

/// <summary>
/// Composition-root helper that registers a context's <see cref="RoomyDbContext"/>-derived
/// <c>DbContext</c> against the Npgsql provider with the project's defaults. Keeping the provider
/// wiring here means EF Core / Npgsql stay an infrastructure concern wired only at composition,
/// never leaking into <c>domain</c>/<c>application</c> (ADR-0005, ADR-0012).
/// </summary>
public static class NpgsqlDbContextRegistration
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> backed by PostgreSQL via Npgsql, with the migrations
    /// assembly defaulted to the context's own assembly.
    /// </summary>
    /// <remarks>
    /// The Npgsql retrying execution strategy is deliberately <em>not</em> enabled: it forbids
    /// user-initiated transactions, which Wolverine's durable transactional outbox/inbox relies on to
    /// commit the aggregate write and the outbox/inbox rows in one transaction (ADR-0005, ADR-0012,
    /// ADR-0037, ADR-0041). Enabling it throws "does not support user-initiated transactions" the moment
    /// an event is actually published or consumed. At-least-once delivery and retry are provided by
    /// Wolverine at the messaging layer; transient database faults surface to the caller instead of being
    /// retried in the data layer.
    /// </remarks>
    /// <typeparam name="TContext">The per-service context deriving from <see cref="RoomyDbContext"/>.</typeparam>
    public static IServiceCollection AddRoomyDbContext<TContext>(this IServiceCollection services, string connectionString)
        where TContext : RoomyDbContext
    {
        Ensure.That((IServiceCollection?)services).IsNotNull();
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.FullName)));

        return services;
    }
}
