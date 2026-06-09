namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;

// Attendance's local mirror of an office's name — master data owned by organization, fed in by the
// OfficeOpened integration event (ADR-0014/0031, 004). The occupancy office rollup names the office
// from here (FR-002), so attendance never joins to organization's database. A plain read-model row,
// rebuildable by replaying the feed.
public sealed class Office
{
    public required Guid OfficeId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Name { get; set; }
}
