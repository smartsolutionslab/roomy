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
    /// Registers <typeparamref name="TContext"/> backed by PostgreSQL via Npgsql, with retry-on-
    /// failure enabled and migrations assembly defaulted to the context's own assembly.
    /// </summary>
    /// <typeparam name="TContext">The per-service context deriving from <see cref="RoomyDbContext"/>.</typeparam>
    public static IServiceCollection AddRoomyDbContext<TContext>(
        this IServiceCollection services,
        string connectionString)
        where TContext : RoomyDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(TContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure();
                }));

        return services;
    }
}
