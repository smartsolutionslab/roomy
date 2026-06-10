namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

// The opaque cursor for the accounts list: the email of the last returned account (ADR-0044). Email
// is unique, so it is a stable total order needing no tiebreaker.
internal sealed record UserCursor(string Email);
