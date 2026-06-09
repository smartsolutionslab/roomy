using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.ValueObjects;

public sealed class IdentifierTests
{
    [Fact]
    public void New_mints_a_non_empty_company_identifier()
    {
        CompanyIdentifier.New().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_rejects_an_empty_office_identifier()
    {
        Should.Throw<ArgumentException>(() => OfficeIdentifier.From(Guid.Empty));
    }

    [Fact]
    public void TryParse_returns_null_for_an_empty_room_identifier()
    {
        RoomIdentifier.TryParse(Guid.Empty).ShouldBeNull();
    }

    [Fact]
    public void Implicit_guid_conversion_round_trips()
    {
        var value = Guid.CreateVersion7();
        OfficeIdentifier identifier = value;
        Guid back = identifier;
        back.ShouldBe(value);
    }
}
