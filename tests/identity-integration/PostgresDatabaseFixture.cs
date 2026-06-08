using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

// Spins up a single real PostgreSQL via Aspire — only the database resource, none of the rest of the
// app graph — and creates the identity schema from the EF model once for the test class. Lets the
// persistence tests exercise the real provider (value converters, unique indexes, NULL semantics)
// against the same Postgres the app host provisions. Requires Docker.
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string ServerResourceName = "postgres";
    private const string DatabaseResourceName = "identity";

    private DistributedApplication? application;
    private string connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Roomy_Identity_TestAppHost>();

        application = await builder.BuildAsync();
        await application.StartAsync();

        // The container is provisioned asynchronously; connect only once it accepts connections,
        // otherwise the first query races the server's startup and fails transiently.
        using var readiness = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var notifications = application.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync(ServerResourceName, readiness.Token);

        connectionString = await application.GetConnectionStringAsync(DatabaseResourceName, readiness.Token)
            ?? throw new InvalidOperationException("The Postgres resource produced no connection string.");

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(readiness.Token);
    }

    public IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
