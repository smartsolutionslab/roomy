using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SmartSolutionsLab.Roomy.TestSupport;

// Provisions a single real PostgreSQL via Aspire — only the database resource from the given test app host,
// none of the rest of the app graph — and exposes the connection string once the server is healthy. The
// schema step (Migrate vs EnsureCreated) is left to the concrete fixture. Requires Docker.
//
// The generic parameter is the per-project generated `Projects.Roomy_*_TestAppHost` metadata type. Aspire
// generates that type into each test project from its app-host ProjectReference, so this shared base needs no
// reference to any app host — it forwards the type into DistributedApplicationTestingBuilder.CreateAsync<T>().
public abstract class BasePostgresFixture<TAppHost> : IAsyncLifetime
    where TAppHost : class
{
    private const string ServerResourceName = "postgres";

    private DistributedApplication? application;

    public string ConnectionString { get; private set; } = string.Empty;

    // The database resource name as declared in the test app host (e.g. "identity", "attendance").
    protected abstract string DatabaseResourceName { get; }

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        // The container is provisioned asynchronously; connect only once it accepts connections, otherwise
        // the first query races the server's startup and fails transiently.
        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(ServerResourceName, readiness.Token);

        ConnectionString = await application.GetConnectionStringAsync(DatabaseResourceName, readiness.Token)
            ?? throw new InvalidOperationException("The Postgres resource produced no connection string.");

        await CreateSchemaAsync(readiness.Token);
    }

    // Builds the schema for this context — Migrate (state-based contexts) or EnsureCreated (event store).
    protected abstract Task CreateSchemaAsync(CancellationToken cancellationToken);

    // Npgsql options over the provisioned database, for a concrete DbContext the fixture constructs.
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
