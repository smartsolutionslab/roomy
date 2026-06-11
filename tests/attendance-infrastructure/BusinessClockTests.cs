using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Tests;

public class BusinessClockTests
{
    private static readonly TimeZoneInfo berlin = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    [Fact]
    public void Today_is_the_local_date_in_the_configured_zone()
    {
        var instant = new DateTimeOffset(2026, 6, 8, 8, 0, 0, TimeSpan.Zero);
        var clock = new BusinessClock(new FixedTimeProvider(instant), berlin);

        clock.Today.ShouldBe(BookingDate.From(new DateOnly(2026, 6, 8)));
    }

    [Fact]
    public void Today_rolls_to_the_next_day_after_local_midnight_in_summer()
    {
        var instant = new DateTimeOffset(2026, 6, 8, 22, 30, 0, TimeSpan.Zero);
        var clock = new BusinessClock(new FixedTimeProvider(instant), berlin);

        clock.Today.ShouldBe(BookingDate.From(new DateOnly(2026, 6, 9)));
    }

    [Fact]
    public void Today_rolls_to_the_next_day_after_local_midnight_in_winter()
    {
        var instant = new DateTimeOffset(2026, 1, 8, 23, 30, 0, TimeSpan.Zero);
        var clock = new BusinessClock(new FixedTimeProvider(instant), berlin);

        clock.Today.ShouldBe(BookingDate.From(new DateOnly(2026, 1, 9)));
    }

    [Fact]
    public void Now_is_the_utc_instant()
    {
        var instant = new DateTimeOffset(2026, 6, 8, 22, 30, 0, TimeSpan.Zero);
        var clock = new BusinessClock(new FixedTimeProvider(instant), berlin);

        clock.Now.ShouldBe(instant);
    }

    [Fact]
    public void A_non_default_zone_yields_a_different_local_date()
    {
        var newYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var instant = new DateTimeOffset(2026, 6, 9, 2, 0, 0, TimeSpan.Zero);

        new BusinessClock(new FixedTimeProvider(instant), newYork).Today
            .ShouldBe(BookingDate.From(new DateOnly(2026, 6, 8)));
    }
}
