using Shouldly;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Tests;

public class BusinessTimeZoneTests
{
    [Fact]
    public void Resolve_defaults_to_Europe_Berlin_when_unset_or_blank()
    {
        BusinessTimeZone.Resolve(null).Id.ShouldBe("Europe/Berlin");
        BusinessTimeZone.Resolve("   ").Id.ShouldBe("Europe/Berlin");
    }

    [Fact]
    public void Resolve_uses_the_configured_zone()
    {
        BusinessTimeZone.Resolve("America/New_York").Id.ShouldBe("America/New_York");
    }

    [Fact]
    public void An_unknown_zone_fails_fast()
    {
        Should.Throw<TimeZoneNotFoundException>(() => BusinessTimeZone.Resolve("Not/AZone"));
    }
}
