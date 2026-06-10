namespace SmartSolutionsLab.Roomy.Organization.Api.Endpoints.Request;

// The POST /offices body. The office is created under the single seeded company, so it carries no
// company id (contract: organization-api.md).
public sealed record CreateOfficeRequest(string Name, string Location);
