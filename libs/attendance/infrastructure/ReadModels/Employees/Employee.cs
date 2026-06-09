namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// Attendance's local link between an identity user and an organization employee — the 1:1 User<->Employee
// relationship (ADR-0025), fed by the EmployeeHired integration event (003 US4). The reserve/cancel
// endpoints resolve the acting user (the token sub) to their EmployeeId from here, so attendance never
// joins to another context's database. The DisplayName is carried so the occupancy view can name the
// employees booked in a room for today and the following day (004 US6, FR-007). A plain read-model row,
// rebuildable by replaying the feed.
public sealed class Employee
{
    public required Guid EmployeeId { get; init; }

    public required Guid UserId { get; init; }

    public required string DisplayName { get; set; }
}
