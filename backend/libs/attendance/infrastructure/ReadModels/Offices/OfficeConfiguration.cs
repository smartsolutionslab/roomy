using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;

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
