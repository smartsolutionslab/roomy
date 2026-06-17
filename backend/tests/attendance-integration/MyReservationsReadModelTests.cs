using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using ReservationRow = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations.Reservation;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

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

        var page = await new MyReservationsReadModel(fixture.CreateDbContext()).GetAsync(EmployeeIdentifier.From(employee), FirstPage, TestContext.Current.CancellationToken);

        var reservations = page.Items;
        reservations.Count.ShouldBe(2);
        reservations[0].Date.Value.ShouldBe(past);
        reservations[1].Date.Value.ShouldBe(future);
        reservations[0].OfficeName.ShouldBe("Munich");
        reservations[0].RoomName.ShouldBe("A1");
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public async Task It_keyset_paginates_by_day_with_a_cursor_to_the_next_page()
    {
        var company = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var employee = Guid.CreateVersion7();
        var days = new[] { new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 3) };

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(new Room { RoomId = roomId, OfficeId = officeId, CompanyId = company, Capacity = 8, Name = "A1" });
            foreach (var day in days)
            {
                seed.Reservations.Add(Reservation(company, employee, officeId, roomId, day));
            }
        });

        var readModel = new MyReservationsReadModel(fixture.CreateDbContext());
        var first = await readModel.GetAsync(
            EmployeeIdentifier.From(employee),
            Page(limit: 2),
            TestContext.Current.CancellationToken);

        first.Items.Select(reservation => reservation.Date.Value).ShouldBe([days[0], days[1]]);
        first.NextCursor.ShouldNotBeNull();

        var second = await readModel.GetAsync(
            EmployeeIdentifier.From(employee),
            Page(limit: 2, cursor: first.NextCursor),
            TestContext.Current.CancellationToken);

        second.Items.Select(reservation => reservation.Date.Value).ShouldBe([days[2]]);
        second.NextCursor.ShouldBeNull();
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

        var page = await new MyReservationsReadModel(fixture.CreateDbContext()).GetAsync(
            EmployeeIdentifier.From(mine),
            FirstPage,
            TestContext.Current.CancellationToken);

        page.Items.ShouldHaveSingleItem().Reservation.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task An_employee_with_no_reservations_gets_an_empty_page()
    {
        var page = await new MyReservationsReadModel(fixture.CreateDbContext()).GetAsync(
            EmployeeIdentifier.New(),
            FirstPage,
            TestContext.Current.CancellationToken);

        page.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }

    private static PageRequest FirstPage => PageRequest.From(cursor: null, limit: null);

    private static PageRequest Page(int limit, string? cursor = null) =>
        PageRequest.From(cursor, limit);

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
