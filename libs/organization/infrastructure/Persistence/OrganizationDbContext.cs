using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : RoomyDbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new OfficeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
    }
}
