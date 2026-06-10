using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Messaging;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Features;

// Use-case tests for GrantAdministrator (US4 / IA-4): elevate an existing account to Administrator,
// syncing the role to Keycloak (the token authority) and persisting the change. Exercised with
// substituted ports; the real Keycloak + Postgres round-trip is the integration/e2e slice.
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
        var users = Substitute.For<IUserRepository>();
        users.GetByIdentifierAsync(Arg.Any<UserIdentifier>(), Arg.Any<CancellationToken>()).Returns(Result.Success(user));
        var assigned = new List<KeycloakSubjectIdentifier>();
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        identityProvider.AssignAdministratorRoleAsync(Arg.Do<KeycloakSubjectIdentifier>(assigned.Add), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        user.IsAdministrator.ShouldBeTrue();
        assigned.ShouldHaveSingleItem().ShouldBe(user.KeycloakSubjectIdentifier!.Value);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

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
        var users = Substitute.For<IUserRepository>();
        users.GetByIdentifierAsync(Arg.Any<UserIdentifier>(), Arg.Any<CancellationToken>()).Returns(Result.Success(user));
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await identityProvider.DidNotReceive().AssignAdministratorRoleAsync(Arg.Any<KeycloakSubjectIdentifier>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Granting_an_unknown_user_returns_not_found()
    {
        var users = Substitute.For<IUserRepository>();
        users.GetByIdentifierAsync(Arg.Any<UserIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(Error.NotFound("user.not_found", "absent")));
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(UserIdentifier.New()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_keycloak_failure_is_returned_and_nothing_is_persisted()
    {
        var user = ActiveEmployee();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdentifierAsync(Arg.Any<UserIdentifier>(), Arg.Any<CancellationToken>()).Returns(Result.Success(user));
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        identityProvider.AssignAdministratorRoleAsync(Arg.Any<KeycloakSubjectIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("provider_error", "sync failed")));
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var result = await Handler(users, identityProvider, unitOfWork)
            .HandleAsync(new GrantAdministrator(user.Identifier), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        user.IsAdministrator.ShouldBeFalse();
        user.DomainEvents.ShouldBeEmpty();
    }
}
