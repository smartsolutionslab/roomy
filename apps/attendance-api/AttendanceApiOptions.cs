namespace SmartSolutionsLab.Roomy.Attendance.Api;

// Host configuration for the attendance context. v1 is single-tenant (ADR-0011): one seeded company,
// so the company that owns every AttendanceDay (ADR-0026) is a configured value rather than resolved
// per request. When multi-tenant lands, the company comes from the authenticated context instead.
public sealed class AttendanceApiOptions
{
    public const string SectionName = "Attendance";

    public Guid CompanyId { get; init; }
}
