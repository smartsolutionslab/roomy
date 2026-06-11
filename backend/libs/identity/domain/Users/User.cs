using SmartSolutionsLab.Roomy.SharedKernel;

namespace SmartSolutionsLab.Roomy.Identity.Domain.Users;

public sealed class User : Aggregate
{
    private User(UserIdentifier identifier, Email email, DisplayName displayName, Role role)
    {
        Identifier = identifier;
        Email = email;
        DisplayName = displayName;
        Role = role;
        Status = UserStatus.Provisioning;
    }

    public UserIdentifier Identifier { get; }
    public Email Email { get; }
    public DisplayName DisplayName { get; }
    public Role Role { get; private set; }
    public KeycloakSubjectIdentifier? KeycloakSubjectIdentifier { get; private set; }
    public UserStatus Status { get; private set; }

    public bool IsEmployee => true;
    public bool IsAdministrator => Role.IsAdministrator;

    public static User Register(Email email, DisplayName displayName, Role role) =>
        Register(UserIdentifier.New(), email, displayName, role);

    public static User Register(
        UserIdentifier identifier,
        Email email,
        DisplayName displayName,
        Role role) =>
        new(identifier, email, displayName, role);

    public void Activate(KeycloakSubjectIdentifier keycloakSubjectIdentifier)
    {
        if (Status != UserStatus.Provisioning) throw new InvalidOperationException("Only a provisioning user can be activated.");

        KeycloakSubjectIdentifier = keycloakSubjectIdentifier;
        Status = UserStatus.Active;
    }

    public void GrantAdministrator(DateTimeOffset occurredAt)
    {
        if (Role.IsAdministrator) return;

        Role = Role.GrantAdministrator();
        RaiseDomainEvent(new AdministratorGranted(Identifier, occurredAt));
    }
}
