namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Offices;

public sealed class Office
{
    public required Guid OfficeId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Name { get; set; }
}
