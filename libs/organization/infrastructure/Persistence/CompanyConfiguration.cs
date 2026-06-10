using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public const string TableName = "Companies";

    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable(TableName);

        builder.Ignore(company => company.DomainEvents);

        builder.HasKey(company => company.Identifier);

        builder.Property(company => company.Identifier)
            .HasConversion(identifier => identifier.Value, value => CompanyIdentifier.From(value))
            .ValueGeneratedNever();

        builder.Property(company => company.Name)
            .HasConversion(name => name.Value, value => CompanyName.From(value))
            .IsRequired();
    }
}
