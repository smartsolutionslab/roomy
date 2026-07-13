using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.Runtime;
using Wolverine.Tracking;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

public sealed class OutboxTransactionOwnershipTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    [Fact]
    public async Task Inside_an_ambient_transaction_it_leaves_the_commit_to_the_caller()
    {
        using var host = await StartWolverineAsync();
        await using var context = fixture.CreateContext();
        // Stand in for Wolverine's AutoApplyTransactions middleware, which owns the transaction and
        // commits it once the handler returns; the adapter must not commit it out from under that.
        await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        await OutboxFor(host).SaveAndPublishAsync(
            context, [new ProbeEvent(Guid.NewGuid())], TestContext.Current.CancellationToken);

        context.Database.CurrentTransaction.ShouldNotBeNull();
        await context.Database.CurrentTransaction!.RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Without_an_ambient_transaction_it_owns_the_flush_through_the_outbox()
    {
        using var host = await StartWolverineAsync();
        await using var context = fixture.CreateContext();
        var integrationEvent = new ProbeEvent(Guid.NewGuid());

        var session = await host.TrackActivity().ExecuteAndWaitAsync(_ =>
            OutboxFor(host).SaveAndPublishAsync(context, [integrationEvent], TestContext.Current.CancellationToken));

        session.Sent.SingleMessage<ProbeEvent>().ShouldBe(integrationEvent);
        context.Database.CurrentTransaction.ShouldBeNull();
    }

    private async Task<IHost> StartWolverineAsync() =>
        await Host.CreateDefaultBuilder()
            .UseWolverine(opts =>
            {
                // Postgres-backed persistence so IDbContextOutbox can enrol and flush (the HTTP path),
                // mirroring the production host; the schema is migrated against the fixture database.
                opts.PersistMessagesWithPostgresql(fixture.ConnectionString);
                opts.Discovery.DisableConventionalDiscovery();
                opts.PublishMessage<ProbeEvent>().ToLocalQueue("outbox-probe");
            })
            .StartAsync();

    private static WolverineIntegrationEventOutbox OutboxFor(IHost host) =>
        new(new DbContextOutbox(host.Services.GetRequiredService<IWolverineRuntime>(), []),
            host.Services.GetRequiredService<IMessageBus>());

    private sealed record ProbeEvent(Guid Id) : IIntegrationEvent;
}
