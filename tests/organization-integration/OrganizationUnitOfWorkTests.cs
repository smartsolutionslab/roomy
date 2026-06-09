using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Organization.IntegrationTests;

// The unit of work realizes ADR-0037: on save it drains the tracked aggregates' domain events, maps them
// to the published contracts (stamping OccurredAt), hands them to the outbox, and clears them. Verified
// with a capturing outbox and a tracking-only context — the drain logic needs no database round-trip.
public sealed class OrganizationUnitOfWorkTests
{
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Saving_drains_office_opened_and_room_added_to_the_outbox_then_clears_them()
    {
        await using var context = NewContext();
        var company = CompanyIdentifier.New();
        var office = Office.Create(company, OfficeName.From("HQ"), Location.From("Berlin"));
        var room = office.AddRoom(RoomName.From("A1"), Capacity.From(8)).Value;
        context.Add(office);
        var outbox = new CapturingOutbox();

        await new OrganizationUnitOfWork(context, outbox, new FixedTimeProvider(occurredAt))
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var opened = outbox.Published.OfType<SmartSolutionsLab.Roomy.Contracts.Organization.OfficeOpened>().ShouldHaveSingleItem();
        opened.OfficeId.ShouldBe(office.Identifier.Value);
        opened.CompanyId.ShouldBe(company.Value);
        opened.Name.ShouldBe("HQ");
        opened.Location.ShouldBe("Berlin");
        opened.OccurredAt.ShouldBe(occurredAt);

        var added = outbox.Published.OfType<SmartSolutionsLab.Roomy.Contracts.Organization.RoomAdded>().ShouldHaveSingleItem();
        added.RoomId.ShouldBe(room.Identifier.Value);
        added.OfficeId.ShouldBe(office.Identifier.Value);
        added.CompanyId.ShouldBe(company.Value);
        added.Capacity.ShouldBe(8);

        office.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Saving_drains_employee_hired_to_the_outbox_with_the_user_and_password()
    {
        await using var context = NewContext();
        var user = UserIdentifier.New();
        var employee = Employee.Hire(
            CompanyIdentifier.New(), user, EmployeeName.From("Ada"), WorkEmail.From("ada@example.com"),
            EmployeeRole.Administrator, "transient-pw");
        context.Add(employee);
        var outbox = new CapturingOutbox();

        await new OrganizationUnitOfWork(context, outbox, new FixedTimeProvider(occurredAt))
            .SaveChangesAsync(TestContext.Current.CancellationToken);

        var hired = outbox.Published.OfType<SmartSolutionsLab.Roomy.Contracts.Organization.EmployeeHired>().ShouldHaveSingleItem();
        hired.EmployeeId.ShouldBe(employee.Identifier.Value);
        hired.UserId.ShouldBe(user.Value);
        hired.Email.ShouldBe("ada@example.com");
        hired.DisplayName.ShouldBe("Ada");
        hired.Role.ShouldBe(SmartSolutionsLab.Roomy.Contracts.Organization.HiredRole.Administrator);
        hired.InitialPassword.ShouldBe("transient-pw");
        hired.OccurredAt.ShouldBe(occurredAt);
        employee.DomainEvents.ShouldBeEmpty();
    }

    private static OrganizationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseNpgsql("Host=unused;Database=unused;Username=unused;Password=unused")
            .Options);

    private sealed class CapturingOutbox : IIntegrationEventOutbox
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task SaveAndPublishAsync(
            DbContext context,
            IReadOnlyCollection<IIntegrationEvent> integrationEvents,
            CancellationToken cancellationToken)
        {
            Published.AddRange(integrationEvents);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
