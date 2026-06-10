using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.DbMigrator;

public static class MigrationTargetServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationTarget<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddSingleton(new MigrationTarget(typeof(TContext)));
        return services;
    }
}
