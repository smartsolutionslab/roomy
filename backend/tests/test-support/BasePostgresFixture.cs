using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SmartSolutionsLab.Roomy.TestSupport;

public abstract class BasePostgresFixture<TAppHost> : IAsyncLifetime
    where TAppHost : class
{
    private const string ServerResourceName = "postgres";

    private DistributedApplication? application;

    public string ConnectionString { get; private set; } = string.Empty;

    protected abstract string DatabaseResourceName { get; }

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(ServerResourceName, readiness.Token);

        ConnectionString = await application.GetConnectionStringAsync(DatabaseResourceName, readiness.Token)
            ?? throw new InvalidOperationException("The Postgres resource produced no connection string.");

        await CreateSchemaAsync(readiness.Token);
    }

    protected abstract Task CreateSchemaAsync(CancellationToken cancellationToken);

    protected DbContextOptions<TContext> NpgsqlOptions<TContext>()
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>().UseNpgsql(ConnectionString).Options;

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
