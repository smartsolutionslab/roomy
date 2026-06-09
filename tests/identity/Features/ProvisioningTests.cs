using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.UseCases;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Features;

// Use-case tests for RegisterUser (US3 / IA-3), the identity step of the provisioning saga (ADR-0025).
// Provisioning is exercised with in-memory doubles for the ports — the real Keycloak + Postgres +
// broker round-trip is covered by the integration/e2e slices once the Aspire stack runs it.
public sealed class ProvisioningTests
{
    private static RegisterUser Command(Role role) =>
        new(
            UserIdentifier.New(),
            EmployeeId: Guid.CreateVersion7(),
            Email.From("ada@example.com"),
            DisplayName.From("Ada Lovelace"),
            role,
            InitialPassword: "correct horse");

    [Fact]
    public async Task Provisioning_an_employee_persists_an_active_account_and_publishes_user_registered()
    {
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());
        var users = new RecordingUserRepository();
        var publisher = new RecordingPublisher();
        var handler = new RegisterUserHandler(
            users, StubIdentityProvider.Succeeds(subject), publisher, TimeProvider.System);
        var command = Command(Role.Employee);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = users.Added.ShouldHaveSingleItem();
        saved.Identifier.ShouldBe(command.UserIdentifier);
        saved.Status.ShouldBe(UserStatus.Active);
        saved.KeycloakSubjectIdentifier.ShouldBe(subject);

        var published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<UserRegistered>();
        published.UserId.ShouldBe(command.UserIdentifier.Value);
        published.EmployeeId.ShouldBe(command.EmployeeId);
        published.Email.ShouldBe("ada@example.com");
        published.Role.ShouldBe(AccountRole.Employee);
        published.KeycloakSubjectId.ShouldBe(subject.Value);
    }

    [Fact]
    public async Task Provisioning_an_administrator_publishes_the_administrator_role()
    {
        var users = new RecordingUserRepository();
        var publisher = new RecordingPublisher();
        var handler = new RegisterUserHandler(
            users,
            StubIdentityProvider.Succeeds(KeycloakSubjectIdentifier.From(Guid.NewGuid())),
            publisher,
            TimeProvider.System);

        await handler.HandleAsync(Command(Role.Employee.GrantAdministrator()), CancellationToken.None);

        var published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<UserRegistered>();
        published.Role.ShouldBe(AccountRole.Administrator);
    }

    [Theory]
    [InlineData("password_rejected", UserProvisioningFailureReason.PasswordRejected)]
    [InlineData("email_taken", UserProvisioningFailureReason.EmailTaken)]
    [InlineData("provider_error", UserProvisioningFailureReason.ProviderError)]
    public async Task Provisioning_failure_publishes_the_reason_and_persists_nothing(
        string providerErrorCode,
        UserProvisioningFailureReason expectedReason)
    {
        var users = new RecordingUserRepository();
        var publisher = new RecordingPublisher();
        var handler = new RegisterUserHandler(
            users,
            StubIdentityProvider.Fails(new Error(providerErrorCode, "provisioning failed")),
            publisher,
            TimeProvider.System);
        var command = Command(Role.Employee);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        users.Added.ShouldBeEmpty();

        var published = publisher.Published.ShouldHaveSingleItem().ShouldBeOfType<UserProvisioningFailed>();
        published.UserId.ShouldBe(command.UserIdentifier.Value);
        published.EmployeeId.ShouldBe(command.EmployeeId);
        published.Reason.ShouldBe(expectedReason);
    }

    private sealed class RecordingUserRepository : IUserRepository
    {
        public List<User> Added { get; } = [];

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Added.Add(user);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Result<User>> GetByIdentifierAsync(
            UserIdentifier identifier,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result<User>>(Error.NotFound("user.not_found", "absent"));

        public Task<Result<User>> GetByKeycloakSubjectAsync(
            KeycloakSubjectIdentifier subject,
            CancellationToken cancellationToken) =>
            Task.FromResult<Result<User>>(Error.NotFound("user.not_found", "absent"));
    }

    private sealed class StubIdentityProvider(Result<KeycloakSubjectIdentifier> outcome) : IIdentityProviderPort
    {
        public static StubIdentityProvider Succeeds(KeycloakSubjectIdentifier subject) => new(subject);

        public static StubIdentityProvider Fails(Error error) => new(error);

        public Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
            Email email,
            DisplayName displayName,
            string initialPassword,
            Role role,
            CancellationToken cancellationToken) =>
            Task.FromResult(outcome);

        public Task<Result> AssignAdministratorRoleAsync(
            KeycloakSubjectIdentifier subject,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Provisioning does not elevate an existing account.");
    }

    private sealed class RecordingPublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
