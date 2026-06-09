using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Application.Ports;
using SmartSolutionsLab.Roomy.Attendance.Application.UseCases;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Application;

// The occupancy view use case: compose per-room figures and the office rollup from the read model's raw
// data, mark full rooms/offices, and apply the data-minimisation policy — names only for today and the
// following day (FR-007). Driven against a stub read model and a fixed clock so "today" is pinned and no
// infrastructure is involved; the SQL itself is covered by the read-model integration tests.
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

        // Three days out: the count is still reported, but the names are withheld (data minimisation).
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

        // A past day is read-only history: its figure is shown, but names are withheld.
        Room(days, today.AddDays(-2)).Occupants.ShouldBeNull();
    }

    [Fact]
    public async Task A_read_model_error_is_propagated()
    {
        var handler = new ViewOccupancyHandler(
            new StubOccupancyReadModel(Error.NotFound("unknown_office", "no such office")),
            new FixedTimeProvider(now));

        var result = await handler.HandleAsync(NewQuery(today, today), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("unknown_office");
    }

    private static async Task<IReadOnlyList<OccupancyView>> ViewAsync(OccupancyData data, DateOnly from, DateOnly to)
    {
        var handler = new ViewOccupancyHandler(new StubOccupancyReadModel(data), new FixedTimeProvider(now));
        var result = await handler.HandleAsync(NewQuery(from, to), CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        return result.Value;
    }

    private static ViewOccupancy NewQuery(DateOnly from, DateOnly to) =>
        new(company, OccupancyScope.ForOffice(office), BookingDate.From(from), BookingDate.From(to));

    private static RoomOccupancy Room(IReadOnlyList<OccupancyView> days, DateOnly date) =>
        days.Single(day => day.Date.Value == date).Rooms.Single();

    private static OccupantRecord[] BookedToday(RoomIdentifier room, int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Booked(room, today))];

    private static OccupantRecord Booked(RoomIdentifier room, DateOnly date) =>
        new(BookingDate.From(date), room, EmployeeIdentifier.New(), "Ada Lovelace");

    private sealed class StubOccupancyReadModel : IOccupancyReadModel
    {
        private readonly Result<OccupancyData> result;

        public StubOccupancyReadModel(OccupancyData data) => result = data;

        public StubOccupancyReadModel(Error error) => result = error;

        public Task<Result<OccupancyData>> GetAsync(
            CompanyIdentifier company,
            OccupancyScope scope,
            BookingDate from,
            BookingDate to,
            CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
