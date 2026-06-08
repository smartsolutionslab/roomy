using SmartSolutionsLab.Roomy.Identity.Domain.ValueObjects;

namespace SmartSolutionsLab.Roomy.Identity.Domain;

// The account/role aggregate — the consistency boundary for one account and its role assignment.
// Every account holds the Employee role; Administrator is an elevation, never standalone
// (FR-001/FR-002). A User is registered as Provisioning and becomes Active only once the Keycloak
// user exists and its role is assigned. Credentials are not modelled here — they live in Keycloak
// (research R1/R2).
public sealed class User
{
    private User(UserId id, Email email, DisplayName displayName, Role role)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Role = role;
        Status = UserStatus.Provisioning;
    }

    public UserId Id { get; }
    public Email Email { get; }
    public DisplayName DisplayName { get; }
    public Role Role { get; }
    public KeycloakSubjectId? KeycloakSubjectId { get; private set; }
    public UserStatus Status { get; private set; }

    // Every account is an employee; Administrator is the Employee role elevated (FR-001/FR-002).
    public bool IsEmployee => true;
    public bool IsAdministrator => Role.IsAdministrator;

    // Registers a new account in the Provisioning state. The Keycloak link and Active status follow
    // once provisioning succeeds (Activate). The value objects carry their own invariants, and the
    // Role guarantees the Employee baseline, so there is nothing further to guard here.
    public static User Register(Email email, DisplayName displayName, Role role) =>
        new(UserId.New(), email, displayName, role);

    // Completes provisioning: links the Keycloak subject and makes the account loginable. Only a
    // Provisioning account can be activated.
    public void Activate(KeycloakSubjectId keycloakSubjectId)
    {
        if (Status != UserStatus.Provisioning)
        {
            throw new InvalidOperationException("Only a provisioning user can be activated.");
        }

        KeycloakSubjectId = keycloakSubjectId;
        Status = UserStatus.Active;
    }
}
