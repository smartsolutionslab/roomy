using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Rooms;
using SmartSolutionsLab.Roomy.Infrastructure.Persistence.EventStore;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

// The attendance context's database (ADR-0014, owned per context). It is event-sourced (ADR-0026), so
// it derives EventStoreDbContext for the append-only events table, and adds the Rooms read model fed by
// organization's RoomAdded integration event (003 US2). The Employees read model joins with US4.
public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : EventStoreDbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    protected override void ConfigureContext(ModelBuilder modelBuilder)
    {
        base.ConfigureContext(modelBuilder);

        modelBuilder.ApplyConfiguration(new RoomConfiguration());
    }
}
