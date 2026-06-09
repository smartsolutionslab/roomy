using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The office-name feed against real PostgreSQL (004): organization's OfficeOpened is mirrored onto the
// local Offices read model by the consumer, so the occupancy rollup can name the office without joining
// to organization's database (ADR-0014/0031). A repeated OfficeOpened refreshes the name in place
// (idempotent), so at-least-once delivery is safe.
public sealed class OfficeNameFeedTests(PostgresEventStoreFixture fixture) : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_office_opened_event_is_mirrored_onto_the_read_model()
    {
        var officeId = Guid.CreateVersion7();
        await ConsumeAsync(new OfficeOpened(officeId, Guid.CreateVersion7(), "Munich", "DE", occurredAt));

        await using var query = fixture.CreateDbContext();
        var stored = await query.Offices
            .SingleAsync(office => office.OfficeId == officeId, TestContext.Current.CancellationToken);

        stored.Name.ShouldBe("Munich");
    }

    [Fact]
    public async Task A_repeated_office_opened_refreshes_the_name_in_place()
    {
        var officeId = Guid.CreateVersion7();
        var company = Guid.CreateVersion7();
        await ConsumeAsync(new OfficeOpened(officeId, company, "Munich", "DE", occurredAt));
        await ConsumeAsync(new OfficeOpened(officeId, company, "München", "DE", occurredAt));

        await using var query = fixture.CreateDbContext();
        var stored = await query.Offices
            .SingleAsync(office => office.OfficeId == officeId, TestContext.Current.CancellationToken);

        stored.Name.ShouldBe("München");
    }

    private async Task ConsumeAsync(OfficeOpened message)
    {
        await using var context = fixture.CreateDbContext();
        await new OfficeOpenedConsumer(context).Handle(message, TestContext.Current.CancellationToken);
    }
}
