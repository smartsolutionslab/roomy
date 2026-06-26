using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries;
using SmartSolutionsLab.Roomy.Attendance.Application.Queries.Handlers;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

public class ViewOccupancyTests
{
    private static readonly DateOnly today = new(2026, 6, 8);
    private static readonly DateTimeOffset now = new(2026, 6, 8, 8, 0, 0, TimeSpan.Zero);
    private static readonly CompanyIdentifier company = CompanyIdentifier.New();
    private static readonly OfficeIdentifier office = OfficeIdentifier.New();

    [Fact]
    public async Task A_rooms_figure_is_its_occupied_places_over_capacity()
    {
        var room = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(room, "A1", RoomCapacity.From(8))],
            BookedToday(room, count: 3));

        var day = (await ViewAsync(data, today, today)).Single();

        var figure = day.Rooms.Single();
        figure.Occupied.ShouldBe(3);
        figure.Capacity.ShouldBe(8);
        figure.IsFull.ShouldBeFalse();
    }

    [Fact]
    public async Task A_full_room_and_office_are_marked_full()
    {
        var room = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(room, "A1", RoomCapacity.From(3))],
            BookedToday(room, count: 3));

        var day = (await ViewAsync(data, today, today)).Single();

        day.Rooms.Single().IsFull.ShouldBeTrue();
        day.Office.IsFull.ShouldBeTrue();
    }

    [Fact]
    public async Task An_office_rollup_sums_its_rooms()
    {
        var roomA = RoomIdentifier.New();
        var roomB = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(roomA, "A1", RoomCapacity.From(20)), new RoomDescriptor(roomB, "B1", RoomCapacity.From(10))],
            [.. BookedToday(roomA, 8), .. BookedToday(roomB, 4)]);

        var day = (await ViewAsync(data, today, today)).Single();

        day.Office.Occupied.ShouldBe(12);
        day.Office.Capacity.ShouldBe(30);
        day.Office.IsFull.ShouldBeFalse();
    }

    [Fact]
    public async Task A_room_with_no_reservations_is_zero_and_still_counts_toward_the_rollup()
    {
        var booked = RoomIdentifier.New();
        var empty = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(booked, "A1", RoomCapacity.From(8)), new RoomDescriptor(empty, "B1", RoomCapacity.From(5))],
            BookedToday(booked, count: 3));

        var day = (await ViewAsync(data, today, today)).Single();

        day.Rooms.Single(room => room.Room == empty).Occupied.ShouldBe(0);
        day.Office.Occupied.ShouldBe(3);
        day.Office.Capacity.ShouldBe(13);
    }

    [Fact]
    public async Task Names_are_shown_only_for_today_and_tomorrow()
    {
        var room = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(room, "A1", RoomCapacity.From(8))],
            [
                Booked(room, today),
                Booked(room, today.AddDays(1)),
                Booked(room, today.AddDays(3)),
            ]);

        var days = await ViewAsync(data, today, today.AddDays(3));

        Room(days, today).Occupants.ShouldNotBeNull();
        Room(days, today.AddDays(1)).Occupants.ShouldNotBeNull();

        var laterDay = Room(days, today.AddDays(3));
        laterDay.Occupied.ShouldBe(1);
        laterDay.Occupants.ShouldBeNull();
    }

    [Fact]
    public async Task Each_day_in_the_range_reports_its_own_figure_including_past_days()
    {
        var room = RoomIdentifier.New();
        var data = new OccupancyData(
            office,
            "Munich",
            [new RoomDescriptor(room, "A1", RoomCapacity.From(8))],
            [Booked(room, today.AddDays(-2)), Booked(room, today), Booked(room, today)]);

        var days = await ViewAsync(data, today.AddDays(-2), today);

        days.Count.ShouldBe(3);
        Room(days, today.AddDays(-2)).Occupied.ShouldBe(1);
        Room(days, today.AddDays(-1)).Occupied.ShouldBe(0);
        Room(days, today).Occupied.ShouldBe(2);

        Room(days, today.AddDays(-2)).Occupants.ShouldBeNull();
    }

    [Fact]
    public async Task A_read_model_error_is_propagated()
    {
        var readModel = Substitute.For<IOccupancyReadModel>();
        readModel.GetAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<OccupancyScope>(), Arg.Any<BookingDateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OccupancyData>(Error.NotFound("unknown_office", "no such office")));
        var handler = new ViewOccupancyHandler(readModel, ClockAt(now, BookingDate.From(today)));

        var result = await handler.HandleAsync(NewQuery(today, today), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_office");
    }

    private static async Task<IReadOnlyList<OccupancyView>> ViewAsync(OccupancyData data, DateOnly from, DateOnly to)
    {
        var readModel = Substitute.For<IOccupancyReadModel>();
        readModel.GetAsync(Arg.Any<CompanyIdentifier>(), Arg.Any<OccupancyScope>(), Arg.Any<BookingDateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(data));
        var handler = new ViewOccupancyHandler(readModel, ClockAt(now, BookingDate.From(today)));
        var result = await handler.HandleAsync(NewQuery(from, to), TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static IBusinessClock ClockAt(DateTimeOffset instant, BookingDate today)
    {
        var clock = Substitute.For<IBusinessClock>();
        clock.Now.Returns(instant);
        clock.Today.Returns(today);
        return clock;
    }

    private static ViewOccupancy NewQuery(DateOnly from, DateOnly to) =>
        new(company, OccupancyScope.ForOffice(office), BookingDateRange.Between(from, to));

    private static RoomOccupancy Room(IReadOnlyList<OccupancyView> days, DateOnly date) =>
        days.Single(day => day.Date.Value == date).Rooms.Single();

    private static OccupantRecord[] BookedToday(RoomIdentifier room, int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Booked(room, today))];

    private static OccupantRecord Booked(RoomIdentifier room, DateOnly date) =>
        new(BookingDate.From(date), room, EmployeeIdentifier.New(), "Ada Lovelace");
}
