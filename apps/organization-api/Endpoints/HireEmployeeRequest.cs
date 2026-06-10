namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints;

internal sealed record HireEmployeeRequest(string DisplayName, string Email, string Role, string InitialPassword);
