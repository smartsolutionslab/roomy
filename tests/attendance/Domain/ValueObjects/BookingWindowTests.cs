using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain.ValueObjects;

public class BookingWindowTests
{
    private static readonly BookingDate monday =
        BookingDate.From(BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));

    private static BookingDate Plus(int days) => BookingDate.From(monday.Value.AddDays(days));

    [Fact]
    public void Today_itself_is_bookable_when_it_is_a_working_day()
    {
        BookingWindow.IsBookable(candidate: monday, today: monday).ShouldBeTrue();
    }

    [Fact]
    public void A_working_day_inside_the_window_is_bookable()
    {
        BookingWindow.IsBookable(candidate: Plus(4), today: monday).ShouldBeTrue();
    }

    [Fact]
    public void The_fourteenth_day_ahead_is_bookable_when_it_is_a_working_day()
    {
        BookingWindow.IsBookable(candidate: Plus(14), today: monday).ShouldBeTrue();
    }

    [Fact]
    public void Saturday_is_not_bookable()
    {
        BookingWindow.IsBookable(candidate: Plus(5), today: monday).ShouldBeFalse();
    }

    [Fact]
    public void Sunday_is_not_bookable()
    {
        BookingWindow.IsBookable(candidate: Plus(6), today: monday).ShouldBeFalse();
    }

    [Fact]
    public void A_working_day_beyond_the_window_is_not_bookable()
    {
        BookingWindow.IsBookable(candidate: Plus(15), today: monday).ShouldBeFalse();
    }

    [Fact]
    public void A_past_working_day_is_not_bookable()
    {
        BookingWindow.IsBookable(candidate: Plus(-3), today: monday).ShouldBeFalse();
    }
}
