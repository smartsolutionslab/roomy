namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Response;

internal sealed record HiredEmployeeResponse(Guid EmployeeId, Guid UserId, string State);
