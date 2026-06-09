using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// Maps the Employees read model to its table (snake_case columns from the shared naming convention).
// Keyed by the organization-side employee id; the unique user id is the lookup the directory resolves on.
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(employee => employee.EmployeeId);
        builder.Property(employee => employee.EmployeeId).ValueGeneratedNever();
        builder.HasIndex(employee => employee.UserId).IsUnique();
    }
}
