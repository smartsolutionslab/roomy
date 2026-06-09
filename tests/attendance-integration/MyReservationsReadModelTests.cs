using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using ReservationRow = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations.Reservation;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The "my reservations" read-model adapter against real PostgreSQL (004 US9): it returns the caller's
// reservations — past and future — ordered by day, named from the local Offices/Rooms read models, and
// excludes other employees' reservations. Another employee's rows must never appear.
public sealed class MyReservationsReadModelTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    [Fact]
    public async Task It_returns_the_employees_reservations_past_and_future_ordered_by_day_with_names()
    {
        var company = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var employee = Guid.CreateVersion7();
        var past = new DateOnly(2026, 6, 1);
        var future = new DateOnly(2026, 6, 20);

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = roomId, OfficeId = officeId, CompanyId = company, Capacity = 8, Name = "A1" });
            seed.Reservations.Add(Reservation(company, employee, officeId, roomId, future));
            seed.Reservations.Add(Reservation(company, employee, officeId, roomId, past));
        });

        var reservations = await new MyReservationsReadModel(fixture.CreateDbContext())
            .GetAsync(EmployeeIdentifier.From(employee), TestContext.Current.CancellationToken);

        reservations.Count.ShouldBe(2);
        reservations[0].Date.Value.ShouldBe(past);
        reservations[1].Date.Value.ShouldBe(future);
        reservations[0].OfficeName.ShouldBe("Munich");
        reservations[0].RoomName.ShouldBe("A1");
    }

    [Fact]
    public async Task It_excludes_another_employees_reservations()
    {
        var company = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var mine = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var date = new DateOnly(2026, 6, 10);

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = roomId, OfficeId = officeId, CompanyId = company, Capacity = 8, Name = "A1" });
            seed.Reservations.Add(Reservation(company, mine, officeId, roomId, date));
            seed.Reservations.Add(Reservation(company, other, officeId, roomId, date));
        });

        var reservations = await new MyReservationsReadModel(fixture.CreateDbContext())
            .GetAsync(EmployeeIdentifier.From(mine), TestContext.Current.CancellationToken);

        reservations.ShouldHaveSingleItem().Reservation.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task An_employee_with_no_reservations_gets_an_empty_list()
    {
        var reservations = await new MyReservationsReadModel(fixture.CreateDbContext())
            .GetAsync(EmployeeIdentifier.New(), TestContext.Current.CancellationToken);

        reservations.ShouldBeEmpty();
    }

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static ReservationRow Reservation(Guid company, Guid employee, Guid officeId, Guid room, DateOnly date) =>
        new()
        {
            ReservationId = Guid.CreateVersion7(),
            CompanyId = company,
            EmployeeId = employee,
            OfficeId = officeId,
            RoomId = room,
            Date = date,
        };
}
