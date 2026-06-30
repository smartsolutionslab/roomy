using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

public static class NpgsqlDbContextRegistration
{
    public static IServiceCollection AddRoomyDbContext<TContext>(this IServiceCollection services, string connectionString)
        where TContext : RoomyDbContext
    {
        Ensure.That((IServiceCollection?)services).IsNotNull();
        Ensure.That(connectionString).IsNotNullOrWhiteSpace();

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(TContext).Assembly.FullName)));

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
