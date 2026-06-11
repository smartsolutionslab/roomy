using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

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
