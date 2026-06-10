namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

public sealed class Employee
{
    public required Guid EmployeeId { get; init; }

    public required Guid UserId { get; init; }

    public required string DisplayName { get; set; }
}
