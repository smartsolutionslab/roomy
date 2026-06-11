using Shouldly;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Tests.Domain.ValueObjects;

public sealed class NameValueObjectTests
{
    [Fact]
    public void CompanyName_trims_and_keeps_the_value()
    {
        CompanyName.From("  Acme  ").Value.ShouldBe("Acme");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CompanyName_rejects_blank(string value)
    {
        Should.Throw<ArgumentException>(() => CompanyName.From(value));
    }

    [Fact]
    public void OfficeName_trims_and_keeps_the_value()
    {
        OfficeName.From("  HQ  ").Value.ShouldBe("HQ");
    }

    [Fact]
    public void OfficeName_rejects_blank()
    {
        Should.Throw<ArgumentException>(() => OfficeName.From("   "));
    }

    [Fact]
    public void Location_trims_and_keeps_the_value()
    {
        Location.From("  Berlin  ").Value.ShouldBe("Berlin");
    }

    [Fact]
    public void Location_rejects_blank()
    {
        Should.Throw<ArgumentException>(() => Location.From(""));
    }

    [Fact]
    public void RoomName_trims_and_keeps_the_value()
    {
        RoomName.From("  Aurora  ").Value.ShouldBe("Aurora");
    }

    [Fact]
    public void RoomName_equality_is_by_value()
    {
        RoomName.From("Aurora").ShouldBe(RoomName.From("Aurora"));
    }
}
