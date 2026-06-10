using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(room => room.RoomId);
        builder.Property(room => room.RoomId).ValueGeneratedNever();
        builder.Property(room => room.Name).IsRequired();
    }
}
