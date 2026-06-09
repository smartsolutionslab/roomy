using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Api;

// Lets `dotnet ef migrations` build the AttendanceDbContext without booting the host (which would try
// to reach Postgres). The connection string is a design-time placeholder — migrations are scaffolded
// from the EF model, not from a live database.
internal sealed class AttendanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AttendanceDbContext>
{
    public AttendanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseNpgsql("Host=localhost;Database=attendance;Username=postgres;Password=postgres")
            .Options;

        return new AttendanceDbContext(options);
    }
}
