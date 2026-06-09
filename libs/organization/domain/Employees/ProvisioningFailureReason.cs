namespace SmartSolutionsLab.Roomy.Organization.Domain.Employees;

// Why a login could not be provisioned (ADR-0025) — a coarse, non-sensitive reason recorded on a failed
// employee. Mirrors identity's UserProvisioningFailureReason, mapped from the published contract at the
// consumer edge (ADR-0031).
public enum ProvisioningFailureReason
{
    EmailTaken,
    PasswordRejected,
    ProviderError,
}
