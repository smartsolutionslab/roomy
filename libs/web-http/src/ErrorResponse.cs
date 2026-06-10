namespace SmartSolutionsLab.Roomy.Web.Http;

// The HTTP error body shared by every context API: the domain error code and a human message, nothing more —
// no stack, no domain detail leaks beyond that. The typed Angular clients are generated from this shape.
public sealed record ErrorResponse(string Code, string Message);
