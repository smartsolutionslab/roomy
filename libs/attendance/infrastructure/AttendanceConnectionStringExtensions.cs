using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

// The attendance context's database connection string, injected by Aspire under the "attendance" resource
// name (ADR-0014). Co-located with the attendance persistence registration so the resource name lives in
// one place.
public static class AttendanceConnectionStringExtensions
{
    public static string GetAttendanceConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString("attendance");
}
