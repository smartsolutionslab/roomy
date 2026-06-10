using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using ReservationRow = SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations.Reservation;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The occupancy read-model adapter against real PostgreSQL (004 US6, ADR-0038): it reads the rooms in
// scope from the Rooms read model and the live reservations from the Reservations projection, naming the
// office from Offices and occupants from Employees — all attendance's own read models. The figures
// themselves are the handler's job (unit-tested); here we prove the SQL returns the right rooms and
// occupant rows, and that an unknown office/room is reported as such.
public sealed class OccupancyReadModelTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly DateOnly date = new(2026, 6, 8);

    [Fact]
    public async Task An_office_scope_returns_its_rooms_and_the_reservations_in_range()
    {
        var company = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var roomA = Guid.CreateVersion7();
        var roomB = Guid.CreateVersion7();
        var employee = Guid.CreateVersion7();

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(Room(roomA, officeId, company, "A1", 8));
            seed.Rooms.Add(Room(roomB, officeId, company, "B1", 5));
            seed.Employees.Add(new Employee { EmployeeId = employee, UserId = Guid.CreateVersion7(), DisplayName = "Ada" });
            seed.Reservations.Add(Reservation(company, employee, officeId, roomA));
        });

        var data = await GetAsync(company, OccupancyScope.ForOffice(OfficeIdentifier.From(officeId)));

        data.OfficeName.ShouldBe("Munich");
        data.Rooms.Count.ShouldBe(2);
        var occupant = data.Occupants.ShouldHaveSingleItem();
        occupant.Room.Value.ShouldBe(roomA);
        occupant.EmployeeName.ShouldBe("Ada");
    }

    [Fact]
    public async Task A_room_scope_returns_only_that_room()
    {
        var company = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var roomA = Guid.CreateVersion7();
        var roomB = Guid.CreateVersion7();

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(Room(roomA, officeId, company, "A1", 8));
            seed.Rooms.Add(Room(roomB, officeId, company, "B1", 5));
        });

        var data = await GetAsync(company, OccupancyScope.ForRoom(RoomIdentifier.From(roomA)));

        var room = data.Rooms.ShouldHaveSingleItem();
        room.Room.Value.ShouldBe(roomA);
        room.Capacity.Value.ShouldBe(8);
    }

    [Fact]
    public async Task An_unknown_office_is_not_found()
    {
        var result = await new OccupancyReadModel(fixture.CreateDbContext()).GetAsync(
            CompanyIdentifier.New(),
            OccupancyScope.ForOffice(OfficeIdentifier.New()),
            BookingDateRange.Between(date, date),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_office");
    }

    [Fact]
    public async Task An_unknown_room_is_not_found()
    {
        var result = await new OccupancyReadModel(fixture.CreateDbContext()).GetAsync(
            CompanyIdentifier.New(),
            OccupancyScope.ForRoom(RoomIdentifier.New()),
            BookingDateRange.Between(date, date),
            TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_room");
    }

    private async Task<OccupancyData> GetAsync(Guid company, OccupancyScope scope)
    {
        await using var query = fixture.CreateDbContext();
        var result = await new OccupancyReadModel(query).GetAsync(
            CompanyIdentifier.From(company),
            scope,
            BookingDateRange.Between(date, date),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Room Room(Guid roomId, Guid officeId, Guid company, string name, int capacity) =>
        new() { RoomId = roomId, OfficeId = officeId, CompanyId = company, Capacity = capacity, Name = name };

    private static ReservationRow Reservation(Guid company, Guid employee, Guid officeId, Guid room) =>
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
