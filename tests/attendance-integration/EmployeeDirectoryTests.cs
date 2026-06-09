using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Messaging;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Contracts.Organization;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The actor→employee feed against real PostgreSQL (003 US4): organization's EmployeeHired is mirrored
// onto the local Employees read model by the consumer, and EmployeeDirectory then resolves the acting
// user (the token sub) to their EmployeeId for authorization (ADR-0014/0031). An unknown user is
// unknown_employee; a repeated EmployeeHired is idempotent.
public sealed class EmployeeDirectoryTests(PostgresEventStoreFixture fixture) : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly DateTimeOffset occurredAt = new(2026, 6, 9, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_employee_hired_event_links_the_user_to_the_employee()
    {
        var employeeId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        await ConsumeAsync(Hired(employeeId, userId));

        await using var query = fixture.CreateDbContext();
        var resolved = await new EmployeeDirectory(query)
            .FindByUserAsync(UserIdentifier.From(userId), TestContext.Current.CancellationToken);

        resolved.IsSuccess.ShouldBeTrue();
        resolved.Value.Value.ShouldBe(employeeId);
    }

    [Fact]
    public async Task An_unknown_user_is_not_found()
    {
        await using var query = fixture.CreateDbContext();

        var resolved = await new EmployeeDirectory(query)
            .FindByUserAsync(UserIdentifier.New(), TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
        resolved.Error.Code.ShouldBe("unknown_employee");
    }

    [Fact]
    public async Task A_repeated_employee_hired_is_idempotent()
    {
        var employeeId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        await ConsumeAsync(Hired(employeeId, userId));
        await ConsumeAsync(Hired(employeeId, userId));

        await using var query = fixture.CreateDbContext();
        var resolved = await new EmployeeDirectory(query)
            .FindByUserAsync(UserIdentifier.From(userId), TestContext.Current.CancellationToken);

        resolved.Value.Value.ShouldBe(employeeId);
    }

    [Fact]
    public async Task An_employee_hired_event_persists_the_display_name()
    {
        var employeeId = Guid.CreateVersion7();
        await ConsumeAsync(Hired(employeeId, Guid.CreateVersion7()));

        await using var query = fixture.CreateDbContext();
        var stored = await query.Employees
            .SingleAsync(employee => employee.EmployeeId == employeeId, TestContext.Current.CancellationToken);

        stored.DisplayName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task A_repeated_employee_hired_refreshes_a_changed_display_name()
    {
        var employeeId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        await ConsumeAsync(new EmployeeHired(
            employeeId, userId, "ada@example.com", "Ada Lovelace", HiredRole.Employee, "pw", occurredAt));
        await ConsumeAsync(new EmployeeHired(
            employeeId, userId, "ada@example.com", "Ada, Countess of Lovelace", HiredRole.Employee, "pw", occurredAt));

        await using var query = fixture.CreateDbContext();
        var stored = await query.Employees
            .SingleAsync(employee => employee.EmployeeId == employeeId, TestContext.Current.CancellationToken);

        stored.DisplayName.ShouldBe("Ada, Countess of Lovelace");
    }

    private static EmployeeHired Hired(Guid employeeId, Guid userId) =>
        new(employeeId, userId, "ada@example.com", "Ada Lovelace", HiredRole.Employee, "transient-pw", occurredAt);

    private async Task ConsumeAsync(EmployeeHired message)
    {
        await using var context = fixture.CreateDbContext();
        await new EmployeeHiredConsumer(context).Handle(message, TestContext.Current.CancellationToken);
    }
}
