using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain.ValueObjects;

// The inclusive [From, To] span the occupancy view is computed over. From must be on or before To; the
// span enumerates every day inclusive and reports its length, both used by the occupancy query.
public class BookingDateRangeTests
{
    private static readonly DateOnly start = new(2026, 6, 1);

    [Fact]
    public void Between_keeps_the_inclusive_endpoints()
    {
        var range = BookingDateRange.Between(start, start.AddDays(3));

        range.From.Value.ShouldBe(start);
        range.To.Value.ShouldBe(start.AddDays(3));
    }

    [Fact]
    public void A_single_day_range_has_length_one()
    {
        BookingDateRange.Between(start, start).LengthInDays.ShouldBe(1);
    }

    [Fact]
    public void Length_counts_both_endpoints()
    {
        BookingDateRange.Between(start, start.AddDays(3)).LengthInDays.ShouldBe(4);
    }

    [Fact]
    public void Days_enumerates_every_day_inclusive_in_order()
    {
        var days = BookingDateRange.Between(start, start.AddDays(2)).Days().ToList();

        days.Select(day => day.Value).ShouldBe([start, start.AddDays(1), start.AddDays(2)]);
    }

    [Fact]
    public void An_inverted_span_has_no_valid_value()
    {
        BookingDateRange.TryParse(start.AddDays(1), start).ShouldBeNull();
    }

    [Fact]
    public void Between_rejects_an_inverted_span()
    {
        Should.Throw<ArgumentException>(() => BookingDateRange.Between(start.AddDays(1), start));
    }
}
