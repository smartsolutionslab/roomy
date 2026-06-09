using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Reservations;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The attendance context's database (ADR-0014, owned per context). It is event-sourced (ADR-0026), so
// it derives EventStoreDbContext for the append-only events table, and adds the read models: Rooms from
// organization's RoomAdded (003 US2, capacity) and Offices from OfficeOpened (004, rollup naming);
// Employees from EmployeeHired (003 US4 + 004 display name); and Reservations, the occupancy projection
// target maintained inline with the event append (ADR-0038).
public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : EventStoreDbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<Office> Offices => Set<Office>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new OfficeConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new ReservationConfiguration());
    }
}
