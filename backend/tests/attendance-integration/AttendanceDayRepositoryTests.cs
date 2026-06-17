using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;
using SmartSolutionsLab.Roomy.TestSupport;

namespace SmartSolutionsLab.Roomy.Attendance.IntegrationTests;

public sealed class AttendanceDayRepositoryTests(PostgresEventStoreFixture fixture)
    : IClassFixture<PostgresEventStoreFixture>
{
    private static readonly BookingDate bookingDate =
        BookingDate.From(BookingDates.FirstMondayOnOrAfter(new DateOnly(2026, 6, 1)));

    private static readonly DateTimeOffset occurredAt = new(2026, 6, 8, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task An_unbooked_day_loads_as_a_fresh_empty_aggregate()
    {
        var day = await fixture.CreateRepository().LoadAsync(CompanyIdentifier.New(), bookingDate, TestContext.Current.CancellationToken);

        day.Reservations.ShouldBeEmpty();
        day.Version.ShouldBe(0);
    }

    [Fact]
    public async Task A_reservation_round_trips_through_the_stream()
    {
        var company = CompanyIdentifier.New();
        var employee = EmployeeIdentifier.New();
        var room = SomeRoom();

        var day = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        day.Reserve(employee, room, RoomCapacity.From(8), bookingDate, occurredAt).IsSuccess.ShouldBeTrue();
        var saved = await fixture.CreateRepository().SaveAsync(day, TestContext.Current.CancellationToken);
        saved.IsSuccess.ShouldBeTrue();

        var reloaded = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);

        reloaded.Version.ShouldBe(1);
        reloaded.Reservations.Count.ShouldBe(1);
        reloaded.Reservations.Single().Employee.ShouldBe(employee);
        reloaded.Reservations.Single().Room.ShouldBe(room.Room);
    }

    [Fact]
    public async Task Two_concurrent_writers_to_the_last_place_cannot_both_commit()
    {
        var company = CompanyIdentifier.New();
        var room = SomeRoom();

        var writerA = fixture.CreateRepository();
        var writerB = fixture.CreateRepository();
        var dayA = await writerA.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        var dayB = await writerB.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        dayA.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), bookingDate, occurredAt);
        dayB.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(1), bookingDate, occurredAt);

        var savedA = await writerA.SaveAsync(dayA, TestContext.Current.CancellationToken);
        var savedB = await writerB.SaveAsync(dayB, TestContext.Current.CancellationToken);

        savedA.IsSuccess.ShouldBeTrue();
        savedB.IsFailure.ShouldBeTrue();
        savedB.Error.Code.ShouldBe("concurrency_conflict");

        var reloaded = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        reloaded.Version.ShouldBe(1);
        reloaded.Reservations.Count.ShouldBe(1);
    }

    [Fact]
    public async Task MutateAsync_reloads_and_retries_after_a_real_concurrency_conflict()
    {
        var company = CompanyIdentifier.New();
        var room = SomeRoom();
        var repository = fixture.CreateRepository();
        var attempts = 0;

        var result = await repository.MutateAsync(
            company,
            bookingDate,
            day =>
            {
                attempts++;
                if (attempts == 1)
                {
                    var rival = fixture.CreateRepository();
                    var rivalDay = rival.LoadAsync(company, bookingDate, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
                    rivalDay.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(8), bookingDate, occurredAt);
                    rival.SaveAsync(rivalDay, TestContext.Current.CancellationToken).GetAwaiter().GetResult();
                }

                return day.Reserve(EmployeeIdentifier.New(), room, RoomCapacity.From(8), bookingDate, occurredAt);
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        attempts.ShouldBe(2);

        var reloaded = await fixture.CreateRepository().LoadAsync(company, bookingDate, TestContext.Current.CancellationToken);
        reloaded.Version.ShouldBe(2);
        reloaded.Reservations.Count.ShouldBe(2);
    }

    private static RoomReference SomeRoom() => RoomReference.From(OfficeIdentifier.New(), RoomIdentifier.New());
}
