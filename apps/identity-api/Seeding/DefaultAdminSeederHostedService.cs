namespace SmartSolutionsLab.Roomy.Identity.Api.Seeding;

// Runs the DefaultAdmin seeding once at startup. The seeder needs scoped services (the DbContext), so
// this singleton hosted service opens a scope to resolve it. Seeding failure fails startup loudly —
// the system must not come up without an administrator (FR-004).
public sealed class DefaultAdminSeederHostedService(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DefaultAdminSeeder>();

        var result = await seeder.SeedAsync(cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"DefaultAdmin seeding failed: {result.Error.Code} — {result.Error.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
