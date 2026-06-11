using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

public static class AttendanceConnectionStringExtensions
{
    public static string GetAttendanceConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("attendance");
}
