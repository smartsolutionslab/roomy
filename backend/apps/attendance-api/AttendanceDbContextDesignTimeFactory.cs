using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartSolutionsLab.Roomy.Attendance.Infrastructure.Persistence;

namespace SmartSolutionsLab.Roomy.Attendance.Api;

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
