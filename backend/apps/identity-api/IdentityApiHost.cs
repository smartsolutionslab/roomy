namespace SmartSolutionsLab.Roomy.Identity.Api;

// Marker type so the WebApplicationFactory integration tests can target this host's assembly. A
// namespaced marker is used instead of the conventional `Program` because the Aspire test app hosts
// referenced by the test project also define a top-level `Program`, which would make the name ambiguous.
public sealed class IdentityApiHost;
