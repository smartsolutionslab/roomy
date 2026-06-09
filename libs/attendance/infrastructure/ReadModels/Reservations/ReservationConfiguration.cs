using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;

// Maps the Reservations read model to its table (snake_case columns from the shared naming convention).
// Keyed by the reservation id; the three indexes serve the read shapes (ADR-0038): (room, date) for the
// per-room count and ranges, (office, date) for the rollup, (employee, date) for "my reservations" and
// the calendar own-day highlight.
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(reservation => reservation.ReservationId);
        builder.Property(reservation => reservation.ReservationId).ValueGeneratedNever();
        builder.HasIndex(reservation => new { reservation.RoomId, reservation.Date });
        builder.HasIndex(reservation => new { reservation.OfficeId, reservation.Date });
        builder.HasIndex(reservation => new { reservation.EmployeeId, reservation.Date });
    }
}
