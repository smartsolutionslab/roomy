using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Attendance.Domain.AttendanceDays;

// The identity of a Reservation: a branded, time-ordered GUIDv7 minted with New() when a place is
// reserved, or From()/TryParse() when replaying the stream. The implicit conversions keep the EF Core
// value converter trivial.
public readonly record struct ReservationIdentifier : IValueObject
{
    public Guid Value { get; private init; }

    public static ReservationIdentifier New() => new() { Value = Guid.CreateVersion7() };

    public static ReservationIdentifier From(Guid value) =>
        TryParse(value) ?? throw new ArgumentException("ReservationIdentifier must not be empty.", nameof(value));

    public static ReservationIdentifier? TryParse(Guid value)
    {
        if (value == Guid.Empty) return null;
        return new() { Value = value };
    }

    public static implicit operator Guid(ReservationIdentifier identifier) => identifier.Value;

    public static implicit operator ReservationIdentifier(Guid value) => From(value);

    public override string ToString() => Value.ToString();
}
