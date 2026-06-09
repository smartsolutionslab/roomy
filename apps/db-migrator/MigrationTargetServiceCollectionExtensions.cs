using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Roomy.DbMigrator;

public static class MigrationTargetServiceCollectionExtensions
{
    // Registers a context's DbContext as a migration target. The context's own persistence registration
    // (e.g. AddIdentityPersistence) still wires the DbContext itself; this only tells the runner to
    // include it in the rollout. Adding a context is this one line plus its persistence registration.
    public static IServiceCollection AddMigrationTarget<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddSingleton(new MigrationTarget(typeof(TContext)));
        return services;
    }
}
