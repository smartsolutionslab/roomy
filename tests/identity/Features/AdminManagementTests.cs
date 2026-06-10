using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.UseCases;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Features;

// Use-case tests for GrantAdministrator (US4 / IA-4): elevate an existing account to Administrator,
// syncing the role to Keycloak (the token authority) and persisting the change. Exercised with
// in-memory port doubles; the real Keycloak + Postgres round-trip is the integration/e2e slice.
public sealed class AdminManagementTests
{
    private static User ActiveEmployee()
    {
        var user = User.Register(
            Email.From("ada@example.com"), DisplayName.From("Ada Lovelace"), Role.Employee);
        user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid()));
        return user;
    }

    private static GrantAdministratorHandler Handler(
        IUserRepository users, IIdentityProviderPort identityProvider, IUnitOfWork unitOfWork) =>
        new(users, identityProvider, unitOfWork, TimeProvider.System);

    [Fact]
    public async Task Granting_an_employee_elevates_syncs_keycloak_persists_and_raises_the_event()
    {
        var user = ActiveEmployee();
        var users = new RecordingUserRepository(user);
        var identityProvider = new RecordingIdentityProvider();
        var unitOfWork = new RecordingUnitOfWork();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        user.IsAdministrator.ShouldBeTrue();
        identityProvider.AssignedSubjects.ShouldHaveSingleItem().ShouldBe(user.KeycloakSubjectIdentifier!.Value);
        unitOfWork.SaveCount.ShouldBe(1);

        var raised = user.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<AdministratorGranted>();
        raised.UserId.ShouldBe(user.Identifier);
    }

    [Fact]
    public async Task Granting_an_existing_administrator_is_an_idempotent_no_op()
    {
        var user = User.Register(
            Email.From("grace@example.com"), DisplayName.From("Grace Hopper"),
            Role.Employee.GrantAdministrator());
        user.Activate(KeycloakSubjectIdentifier.From(Guid.NewGuid()));
        var users = new RecordingUserRepository(user);
        var identityProvider = new RecordingIdentityProvider();
        var unitOfWork = new RecordingUnitOfWork();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        identityProvider.AssignedSubjects.ShouldBeEmpty();
        unitOfWork.SaveCount.ShouldBe(0);
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Granting_an_unknown_user_returns_not_found()
    {
        var users = new RecordingUserRepository();
        var identityProvider = new RecordingIdentityProvider();
        var unitOfWork = new RecordingUnitOfWork();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(UserIdentifier.New()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_keycloak_failure_is_returned_and_nothing_is_persisted()
    {
        var user = ActiveEmployee();
        var users = new RecordingUserRepository(user);
        var identityProvider = new RecordingIdentityProvider(new Error("provider_error", "sync failed"));
        var unitOfWork = new RecordingUnitOfWork();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        unitOfWork.SaveCount.ShouldBe(0);
        user.IsAdministrator.ShouldBeFalse();
        user.DomainEvents.ShouldBeEmpty();
    }

    private sealed class RecordingUserRepository(params User[] seeded) : IUserRepository
    {
        private readonly Dictionary<UserIdentifier, User> users =
            seeded.ToDictionary(user => user.Identifier);

        public Task<Result<User>> GetByIdentifierAsync(
            UserIdentifier identifier, CancellationToken cancellationToken) =>
            Task.FromResult<Result<User>>(
                users.TryGetValue(identifier, out var user)
                    ? user
                    : Error.NotFound("user.not_found", "absent"));

        public Task<Result<User>> GetByKeycloakSubjectAsync(
            KeycloakSubjectIdentifier subject, CancellationToken cancellationToken) =>
            Task.FromResult<Result<User>>(Error.NotFound("user.not_found", "absent"));

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<Result<Page<User>>> GetPageAsync(
            PageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<Result<Page<User>>>(new Page<User>(users.Values.ToList(), null));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            users[user.Identifier] = user;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingIdentityProvider(Error? assignmentError = null) : IIdentityProviderPort
    {
        public List<KeycloakSubjectIdentifier> AssignedSubjects { get; } = [];

        public Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
            Email email, DisplayName displayName, string initialPassword, Role role,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Provisioning is not part of the elevation use case.");

        public Task<Result> AssignAdministratorRoleAsync(
            KeycloakSubjectIdentifier subject, CancellationToken cancellationToken)
        {
            if (assignmentError is not null)
            {
                return Task.FromResult<Result>(assignmentError);
            }

            AssignedSubjects.Add(subject);
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
