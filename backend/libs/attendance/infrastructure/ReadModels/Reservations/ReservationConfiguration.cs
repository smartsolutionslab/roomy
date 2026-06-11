using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;

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
