using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public const string TableName = "Employees";
    public const string UserIndexName = "UX_Employees_UserIdentifier";

    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable(TableName);

        builder.Ignore(employee => employee.DomainEvents);

        builder.HasKey(employee => employee.Identifier);

        builder.Property(employee => employee.Identifier)
            .HasConversion(identifier => identifier.Value, value => EmployeeIdentifier.From(value))
            .ValueGeneratedNever();

        builder.Property(employee => employee.CompanyIdentifier)
            .HasConversion(identifier => identifier.Value, value => CompanyIdentifier.From(value))
            .IsRequired();

        builder.Property(employee => employee.UserIdentifier)
            .HasConversion(identifier => identifier.Value, value => UserIdentifier.From(value))
            .IsRequired();

        builder.Property(employee => employee.Name)
            .HasConversion(name => name.Value, value => EmployeeName.From(value))
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasConversion(email => email.Value, value => WorkEmail.From(value))
            .IsRequired();

        builder.Property(employee => employee.Role)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(employee => employee.State)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(employee => employee.FailureReason)
            .HasConversion<string>();

        builder.HasIndex(employee => employee.UserIdentifier)
            .IsUnique()
            .HasDatabaseName(UserIndexName);
    }
}
