using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

// EF Core mapping for the Office aggregate. Rooms are mapped as an owned collection in their own
// table, so they load with their office and are reached only through it (the aggregate boundary).
// Two invariants live at the database as unique indexes: office names are unique within the company
// (FR-010) and room names are unique within their office. The derived office capacity is not stored.
public sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public const string TableName = "Offices";
    public const string RoomsTableName = "Rooms";
    public const string OfficeNameIndexName = "UX_Offices_CompanyIdentifier_Name";
    public const string RoomNameIndexName = "UX_Rooms_OfficeIdentifier_Name";
    private const string RoomOwnerForeignKey = "OfficeIdentifier";

    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable(TableName);

        builder.Ignore(office => office.DomainEvents);
        builder.Ignore(office => office.Capacity);

        builder.HasKey(office => office.Identifier);

        builder.Property(office => office.Identifier)
            .HasConversion(identifier => identifier.Value, value => OfficeIdentifier.From(value))
            .ValueGeneratedNever();

        builder.Property(office => office.CompanyIdentifier)
            .HasConversion(identifier => identifier.Value, value => CompanyIdentifier.From(value))
            .IsRequired();

        builder.Property(office => office.Name)
            .HasConversion(name => name.Value, value => OfficeName.From(value))
            .IsRequired();

        builder.Property(office => office.Location)
            .HasConversion(location => location.Value, value => Location.From(value))
            .IsRequired();

        builder.HasIndex(office => new { office.CompanyIdentifier, office.Name })
            .IsUnique()
            .HasDatabaseName(OfficeNameIndexName);

        builder.OwnsMany(office => office.Rooms, room =>
        {
            room.ToTable(RoomsTableName);
            room.WithOwner().HasForeignKey(RoomOwnerForeignKey);

            room.HasKey(entity => entity.Identifier);

            room.Property(entity => entity.Identifier)
                .HasConversion(identifier => identifier.Value, value => RoomIdentifier.From(value))
                .ValueGeneratedNever();

            room.Property(entity => entity.Name)
                .HasConversion(name => name.Value, value => RoomName.From(value))
                .IsRequired();

            room.Property(entity => entity.Capacity)
                .HasConversion(capacity => capacity.Value, value => Capacity.From(value))
                .IsRequired();

            room.HasIndex(RoomOwnerForeignKey, nameof(Room.Name))
                .IsUnique()
                .HasDatabaseName(RoomNameIndexName);
        });

        builder.Navigation(office => office.Rooms).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
