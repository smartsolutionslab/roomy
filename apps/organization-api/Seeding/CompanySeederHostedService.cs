using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Roomy.Organization.Api.Seeding;

// Runs the company seeding once at startup. The seeder needs scoped services (the DbContext), so this
// singleton hosted service opens a scope to resolve it. A seeding failure fails startup loudly.
public sealed class CompanySeederHostedService(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CompanySeeder>();
        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
