namespace SmartSolutionsLab.Roomy.Gateway.Bff.Response;

// The minimal identity projection the SPA needs: who is signed in and what they may do.
// Deliberately free of any token material — the BFF never leaks tokens to the browser.
public sealed record CurrentUser(string Name, IReadOnlyList<string> Roles);
