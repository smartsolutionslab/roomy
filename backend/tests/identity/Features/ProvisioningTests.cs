using NSubstitute;
using Shouldly;
using SmartSolutionsLab.Roomy.Application.Contracts.Integration;
using SmartSolutionsLab.Roomy.Contracts.Identity;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Application.Commands;
using SmartSolutionsLab.Roomy.Identity.Application.Commands.Handlers;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Tests.Features;

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
        var added = new List<User>();
        var users = Substitute.For<IUserRepository>();
        _ = users.AddAsync(Arg.Do<User>(added.Add), Arg.Any<CancellationToken>());
        var published = new List<IIntegrationEvent>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        _ = publisher.PublishAsync(Arg.Do<IIntegrationEvent>(published.Add), Arg.Any<CancellationToken>());
        var handler = new RegisterUserHandler(
            users, IdentityProviderSucceeding(subject), publisher, TimeProvider.System);
        var command = Command(Role.Employee);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saved = added.ShouldHaveSingleItem();
        saved.Identifier.ShouldBe(command.UserIdentifier);
        saved.Status.ShouldBe(UserStatus.Active);
        saved.KeycloakSubjectIdentifier.ShouldBe(subject);

        var registered = published.ShouldHaveSingleItem().ShouldBeOfType<UserRegistered>();
        registered.UserId.ShouldBe(command.UserIdentifier.Value);
        registered.EmployeeId.ShouldBe(command.EmployeeId);
        registered.Email.ShouldBe("ada@example.com");
        registered.Role.ShouldBe(AccountRole.Employee);
        registered.KeycloakSubjectId.ShouldBe(subject.Value);
    }

    [Fact]
    public async Task Provisioning_an_administrator_publishes_the_administrator_role()
    {
        var published = new List<IIntegrationEvent>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        _ = publisher.PublishAsync(Arg.Do<IIntegrationEvent>(published.Add), Arg.Any<CancellationToken>());
        var handler = new RegisterUserHandler(
            Substitute.For<IUserRepository>(),
            IdentityProviderSucceeding(KeycloakSubjectIdentifier.From(Guid.NewGuid())),
            publisher,
            TimeProvider.System);

        await handler.HandleAsync(Command(Role.Employee.GrantAdministrator()), CancellationToken.None);

        var registered = published.ShouldHaveSingleItem().ShouldBeOfType<UserRegistered>();
        registered.Role.ShouldBe(AccountRole.Administrator);
    }

    [Theory]
    [InlineData("password_rejected", UserProvisioningFailureReason.PasswordRejected)]
    [InlineData("email_taken", UserProvisioningFailureReason.EmailTaken)]
    [InlineData("provider_error", UserProvisioningFailureReason.ProviderError)]
    public async Task Provisioning_failure_publishes_the_reason_and_persists_nothing(
        string providerErrorCode,
        UserProvisioningFailureReason expectedReason)
    {
        var users = Substitute.For<IUserRepository>();
        var published = new List<IIntegrationEvent>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        _ = publisher.PublishAsync(Arg.Do<IIntegrationEvent>(published.Add), Arg.Any<CancellationToken>());
        var handler = new RegisterUserHandler(
            users,
            IdentityProviderFailing(new Error(providerErrorCode, "provisioning failed")),
            publisher,
            TimeProvider.System);
        var command = Command(Role.Employee);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

        var failed = published.ShouldHaveSingleItem().ShouldBeOfType<UserProvisioningFailed>();
        failed.UserId.ShouldBe(command.UserIdentifier.Value);
        failed.EmployeeId.ShouldBe(command.EmployeeId);
        failed.Reason.ShouldBe(expectedReason);
    }

    private static IIdentityProviderPort IdentityProviderSucceeding(KeycloakSubjectIdentifier subject)
    {
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        identityProvider.ProvisionUserAsync(
                Arg.Any<Email>(), Arg.Any<DisplayName>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(subject));
        return identityProvider;
    }

    private static IIdentityProviderPort IdentityProviderFailing(Error error)
    {
        var identityProvider = Substitute.For<IIdentityProviderPort>();
        identityProvider.ProvisionUserAsync(
                Arg.Any<Email>(), Arg.Any<DisplayName>(), Arg.Any<string>(), Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeycloakSubjectIdentifier>(error));
        return identityProvider;
    }
}
