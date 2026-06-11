using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.Offices;

public sealed class OfficeDomainEventTests
{
    [Fact]
    public void Creating_an_office_raises_office_opened()
    {
        var company = CompanyIdentifier.New();

        var office = Office.Create(company, OfficeName.From("HQ"), Location.From("Berlin"));

        var opened = office.DomainEvents.OfType<OfficeOpened>().ShouldHaveSingleItem();
        opened.Office.ShouldBe(office.Identifier);
        opened.Company.ShouldBe(company);
        opened.Name.ShouldBe(office.Name);
        opened.Location.ShouldBe(office.Location);
    }

    [Fact]
    public void Adding_a_room_raises_room_added()
    {
        var office = Office.Create(CompanyIdentifier.New(), OfficeName.From("HQ"), Location.From("Berlin"));
        office.ClearDomainEvents();

        var result = office.AddRoom(RoomName.From("A1"), Capacity.From(8));

        result.IsSuccess.ShouldBeTrue();
        var added = office.DomainEvents.OfType<RoomAdded>().ShouldHaveSingleItem();
        added.Room.ShouldBe(result.Value.Identifier);
        added.Office.ShouldBe(office.Identifier);
        added.Company.ShouldBe(office.CompanyIdentifier);
        added.Name.ShouldBe(result.Value.Name);
        added.Capacity.ShouldBe(result.Value.Capacity);
    }

    [Fact]
    public void A_rejected_room_raises_no_event()
    {
        var office = Office.Create(CompanyIdentifier.New(), OfficeName.From("HQ"), Location.From("Berlin"));
        office.AddRoom(RoomName.From("A1"), Capacity.From(8));
        office.ClearDomainEvents();

        var duplicate = office.AddRoom(RoomName.From("A1"), Capacity.From(4));

        duplicate.IsFailure.ShouldBeTrue();
        office.DomainEvents.ShouldBeEmpty();
    }
}
