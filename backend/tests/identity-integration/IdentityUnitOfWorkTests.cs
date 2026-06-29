using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class IdentityUnitOfWorkTests
{
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Saving_drains_administrator_granted_to_the_outbox_then_clears_it()
    {
        await using var context = NewContext();
        var user = User.Register(Email.From("ada@example.com"), DisplayName.From("Ada"), Role.Employee);
        user.GrantAdministrator(occurredAt);
        context.Add(user);
        var outbox = new CapturingOutbox();

        await new IdentityUnitOfWork(context, outbox, new FixedTimeProvider(occurredAt)).SaveChangesAsync(TestContext.Current.CancellationToken);

        var granted = outbox.Published.OfType<SmartSolutionsLab.Roomy.Contracts.Identity.AdministratorGranted>().ShouldHaveSingleItem();
        granted.UserId.ShouldBe(user.Identifier.Value);
        granted.OccurredAt.ShouldBe(occurredAt);
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Saving_with_no_domain_event_publishes_nothing()
    {
        await using var context = NewContext();
        var user = User.Register(Email.From("noop@example.com"), DisplayName.From("Noop"), Role.Employee);
        context.Add(user);
        var outbox = new CapturingOutbox();

        await new IdentityUnitOfWork(context, outbox, new FixedTimeProvider(occurredAt)).SaveChangesAsync(TestContext.Current.CancellationToken);

        outbox.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_second_save_does_not_republish_an_already_drained_event()
    {
        await using var context = NewContext();
        var user = User.Register(Email.From("ada@example.com"), DisplayName.From("Ada"), Role.Employee);
        user.GrantAdministrator(occurredAt);
        context.Add(user);
        var outbox = new CapturingOutbox();
        var unitOfWork = new IdentityUnitOfWork(context, outbox, new FixedTimeProvider(occurredAt));

        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        outbox.Published.OfType<SmartSolutionsLab.Roomy.Contracts.Identity.AdministratorGranted>().ShouldHaveSingleItem();
    }

    private static IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=unused;Database=unused;Username=unused;Password=unused")
            .Options);

    private sealed class CapturingOutbox : IIntegrationEventOutbox
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task SaveAndPublishAsync(DbContext context, IReadOnlyCollection<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken)
        {
            Published.AddRange(integrationEvents);
            return Task.CompletedTask;
        }
    }
}
