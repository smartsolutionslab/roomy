namespace SmartSolutionsLab.Roomy.Attendance.Infrastructure.ReadModels.Employees;

// The projected search row: the employee plus its computed word-similarity, used to build the next cursor.
internal sealed record EmployeeSearchRow(Guid EmployeeId, string DisplayName, double Similarity);
