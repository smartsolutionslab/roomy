namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure;

public static class BusinessTimeZone
{
    public const string DefaultId = "Europe/Berlin";

    public static TimeZoneInfo Resolve(string? configuredId) =>
        TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(configuredId) ? DefaultId : configuredId);
}
