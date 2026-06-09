using Shouldly;
using SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

namespace SmartSolutionsLab.Roomy.Attendance.Tests.Domain.ValueObjects;

// The attendance context's branded identifiers and small value objects (no primitive obsession,
// CLAUDE.md / data-model.md). Identifiers are GUIDv7, non-empty, with implicit Guid conversions
// for EF; the others carry their own invariants.
public class IdentifierAndValueObjectTests
{
    [Fact]
    public void New_reservation_identifier_is_a_non_empty_guid()
    {
        ReservationIdentifier.New().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void New_reservation_identifiers_are_unique()
    {
        ReservationIdentifier.New().ShouldNotBe(ReservationIdentifier.New());
    }

    [Fact]
    public void From_round_trips_a_guid_through_each_identifier()
    {
        var value = Guid.CreateVersion7();

        ((Guid)CompanyIdentifier.From(value)).ShouldBe(value);
        ((Guid)EmployeeIdentifier.From(value)).ShouldBe(value);
        ((Guid)OfficeIdentifier.From(value)).ShouldBe(value);
        ((Guid)RoomIdentifier.From(value)).ShouldBe(value);
        ((Guid)ReservationIdentifier.From(value)).ShouldBe(value);
        ((Guid)UserIdentifier.From(value)).ShouldBe(value);
    }

    [Fact]
    public void Empty_guids_are_rejected_by_every_identifier()
    {
        Should.Throw<ArgumentException>(() => CompanyIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => EmployeeIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => OfficeIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => RoomIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => ReservationIdentifier.From(Guid.Empty));
        Should.Throw<ArgumentException>(() => UserIdentifier.From(Guid.Empty));
    }

    [Fact]
    public void Identifiers_equal_by_value()
    {
        var value = Guid.CreateVersion7();

        EmployeeIdentifier.From(value).ShouldBe(EmployeeIdentifier.From(value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(500)]
    public void Room_capacity_accepts_one_or_more_places(int places)
    {
        RoomCapacity.From(places).Value.ShouldBe(places);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Room_capacity_rejects_fewer_than_one_place(int places)
    {
        Should.Throw<ArgumentException>(() => RoomCapacity.From(places));
    }

    [Fact]
    public void A_booking_date_carries_its_calendar_day()
    {
        var day = new DateOnly(2026, 6, 9);

        BookingDate.From(day).Value.ShouldBe(day);
    }

    [Fact]
    public void A_room_reference_pairs_a_room_with_its_office()
    {
        var office = OfficeIdentifier.New();
        var room = RoomIdentifier.New();

        var reference = RoomReference.From(office, room);

        reference.Office.ShouldBe(office);
        reference.Room.ShouldBe(room);
    }
}
