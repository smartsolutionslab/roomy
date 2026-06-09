using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The attendance context's database (ADR-0014, owned per context). It is event-sourced (ADR-0026), so
// it derives EventStoreDbContext for the append-only events table, and adds the read models fed by
// integration events: Rooms from organization's RoomAdded (003 US2, capacity) and Employees from
// EmployeeHired (003 US4, actor->employee resolution).
public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : EventStoreDbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
    }
}
