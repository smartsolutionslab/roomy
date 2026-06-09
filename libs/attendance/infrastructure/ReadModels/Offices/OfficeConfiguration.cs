using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;

// Maps the Offices read model to its table (snake_case columns from the shared naming convention).
// Keyed by the organization-side office id; the name is updated in place as OfficeOpened arrives.
internal sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable("offices");
        builder.HasKey(office => office.OfficeId);
        builder.Property(office => office.OfficeId).ValueGeneratedNever();
        builder.Property(office => office.Name).IsRequired();
    }
}
