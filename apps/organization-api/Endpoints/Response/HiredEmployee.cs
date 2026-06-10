namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;

internal sealed record HiredEmployee(
    Guid EmployeeId,
    Guid UserId,
    string State);
