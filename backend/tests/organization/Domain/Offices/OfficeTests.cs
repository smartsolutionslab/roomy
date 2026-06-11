using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.Offices;

public sealed class OfficeTests
{
    private static Office NewOffice() =>
        Office.Create(CompanyIdentifier.New(), OfficeName.From("HQ"), Location.From("Berlin"));

    [Fact]
    public void Create_starts_with_no_rooms_and_zero_capacity()
    {
        var office = NewOffice();

        office.Rooms.ShouldBeEmpty();
        office.Capacity.ShouldBe(0);
    }

    [Fact]
    public void Rename_changes_the_name()
    {
        var office = NewOffice();

        office.Rename(OfficeName.From("Headquarters"));

        office.Name.Value.ShouldBe("Headquarters");
    }

    [Fact]
    public void RelocateTo_changes_the_location()
    {
        var office = NewOffice();

        office.RelocateTo(Location.From("Munich"));

        office.Location.Value.ShouldBe("Munich");
    }

    [Fact]
    public void AddRoom_appends_a_room_and_contributes_to_capacity()
    {
        var office = NewOffice();

        var result = office.AddRoom(RoomName.From("Aurora"), Capacity.From(8));

        result.IsSuccess.ShouldBeTrue();
        office.Rooms.ShouldHaveSingleItem();
        office.Capacity.ShouldBe(8);
    }

    [Fact]
    public void Capacity_is_the_sum_of_room_capacities()
    {
        var office = NewOffice();
        office.AddRoom(RoomName.From("Aurora"), Capacity.From(8));
        office.AddRoom(RoomName.From("Borealis"), Capacity.From(4));

        office.Capacity.ShouldBe(12);
    }

    [Fact]
    public void AddRoom_rejects_a_duplicate_room_name()
    {
        var office = NewOffice();
        office.AddRoom(RoomName.From("Aurora"), Capacity.From(8));

        var result = office.AddRoom(RoomName.From("Aurora"), Capacity.From(4));

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        office.Rooms.Count.ShouldBe(1);
    }

    [Fact]
    public void RenameRoom_changes_the_room_name()
    {
        var office = NewOffice();
        var room = office.AddRoom(RoomName.From("Aurora"), Capacity.From(8)).Value;

        var result = office.RenameRoom(room.Identifier, RoomName.From("Polaris"));

        result.IsSuccess.ShouldBeTrue();
        office.Rooms.Single().Name.Value.ShouldBe("Polaris");
    }

    [Fact]
    public void RenameRoom_returns_not_found_for_an_unknown_room()
    {
        var office = NewOffice();

        var result = office.RenameRoom(RoomIdentifier.New(), RoomName.From("Polaris"));

        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void RenameRoom_rejects_a_duplicate_name_within_the_office()
    {
        var office = NewOffice();
        office.AddRoom(RoomName.From("Aurora"), Capacity.From(8));
        var second = office.AddRoom(RoomName.From("Borealis"), Capacity.From(4)).Value;

        var result = office.RenameRoom(second.Identifier, RoomName.From("Aurora"));

        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }
}
