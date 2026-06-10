namespace SmartSolutionsLab.Roomy.Gateway.Bff.Response;

public sealed record CurrentUser(string Name, IReadOnlyList<string> Roles);
