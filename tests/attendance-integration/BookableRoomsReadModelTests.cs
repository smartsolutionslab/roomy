using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

// The bookable-rooms read-model adapter against real PostgreSQL (007 US1): it lists the company's rooms
// from the Rooms read model, joined to Offices for their names — attendance's own read models. Here we
// prove the SQL returns the company's rooms with their office names, scopes to the company, and that a
// room whose office feed has not arrived is still listed (it is bookable) under an empty office name.
public sealed class BookableRoomsReadModelTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    [Fact]
    public async Task It_lists_the_companys_rooms_with_their_office_names()
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

        var rooms = await GetAsync(company);

        rooms.Count.ShouldBe(2);
        rooms.ShouldAllBe(room => room.OfficeName == "Munich");
        rooms.Select(room => room.Room.Value).ShouldBe([roomA, roomB], ignoreOrder: true);
        rooms.Single(room => room.Room.Value == roomA).Capacity.Value.ShouldBe(8);
    }

    [Fact]
    public async Task It_scopes_to_the_company()
    {
        var company = Guid.CreateVersion7();
        var otherCompany = Guid.CreateVersion7();
        var officeId = Guid.CreateVersion7();
        var mine = Guid.CreateVersion7();

        await SeedAsync(seed =>
        {
            seed.Offices.Add(new Office { OfficeId = officeId, CompanyId = company, Name = "Munich" });
            seed.Rooms.Add(Room(mine, officeId, company, "A1", 8));
            seed.Rooms.Add(Room(Guid.CreateVersion7(), Guid.CreateVersion7(), otherCompany, "Z9", 4));
        });

        var rooms = await GetAsync(company);

        rooms.ShouldHaveSingleItem().Room.Value.ShouldBe(mine);
    }

    [Fact]
    public async Task A_room_whose_office_is_unknown_is_still_listed_with_an_empty_office_name()
    {
        var company = Guid.CreateVersion7();
        var room = Guid.CreateVersion7();

        await SeedAsync(seed => seed.Rooms.Add(Room(room, Guid.CreateVersion7(), company, "A1", 8)));

        var rooms = await GetAsync(company);

        var listed = rooms.ShouldHaveSingleItem();
        listed.Room.Value.ShouldBe(room);
        listed.OfficeName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task A_company_with_no_rooms_gets_an_empty_list()
    {
        var rooms = await GetAsync(Guid.CreateVersion7());

        rooms.ShouldBeEmpty();
    }

    private async Task<IReadOnlyList<BookableRoomView>> GetAsync(Guid company)
    {
        await using var query = fixture.CreateDbContext();
        return await new BookableRoomsReadModel(query).GetAsync(
            CompanyIdentifier.From(company), TestContext.Current.CancellationToken);
    }

    private async Task SeedAsync(Action<AttendanceDbContext> seed)
    {
        await using var context = fixture.CreateDbContext();
        seed(context);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Room Room(Guid roomId, Guid officeId, Guid company, string name, int capacity) =>
        new() { RoomId = roomId, OfficeId = officeId, CompanyId = company, Capacity = capacity, Name = name };
}
