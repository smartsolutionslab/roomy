using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;

// Maps the Rooms read model to its table (snake_case columns come from the shared naming convention).
// Keyed by the organization-side room id; capacity and name are updated in place as RoomAdded arrives.
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
