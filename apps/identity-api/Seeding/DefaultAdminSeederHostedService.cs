namespace SmartSolutionsLab.Roomy.Identity.Api.Seeding;

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
