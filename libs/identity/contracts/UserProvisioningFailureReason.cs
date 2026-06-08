namespace SmartSolutionsLab.Roomy.Contracts.Identity;

// Coarse, non-sensitive reason an account could not be provisioned — it drives saga compensation in
// the organization context (ADR-0025) without leaking provider detail. These mirror the failure codes
// the identity-provider port returns (email_taken / password_rejected / provider_error).
public enum UserProvisioningFailureReason
{
    EmailTaken,
    PasswordRejected,
    ProviderError,
}
