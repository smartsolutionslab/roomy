namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

internal sealed record EmployeeSearchRow(
    Guid EmployeeId,
    string DisplayName,
    double Similarity);
