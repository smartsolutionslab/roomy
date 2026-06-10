namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

internal sealed record HiredEmployeeResponse(Guid EmployeeId, Guid UserId, string State);
