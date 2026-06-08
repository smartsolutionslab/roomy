using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Identity.Api.Hosting;

// Applies the EF migrations at startup so the schema exists before anything reads it — the DefaultAdmin
// seeder runs straight after and queries the users table. Registered before the seeder so it runs
// first. Single-instance dev/MVP convention; production schema rollout is a separate ops concern.
public sealed class IdentityDatabaseMigrator(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
