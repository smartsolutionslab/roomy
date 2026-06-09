using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;
using SmartSolutionsLab.Roomy.Organization.Domain.Companies;
using SmartSolutionsLab.Roomy.Organization.Domain.Employees;
using SmartSolutionsLab.Roomy.Organization.Domain.Offices;

namespace SmartSolutionsLab.Roomy.Organization.Infrastructure.Persistence;

// The organization service's own database (ADR-0014). Derives the shared RoomyDbContext baseline for
// snake_case naming. Rooms are an owned collection of Office, so they have no DbSet — they are only
// reached through their office. Employees are hired here and provisioned via the saga (008, ADR-0025).
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
