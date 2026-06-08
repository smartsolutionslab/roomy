using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class PersistenceTests(PostgresDatabaseFixture fixture) : IClassFixture<PostgresDatabaseFixture>
{
    private static User RegisterEmployee(string email) =>
        User.Register(Email.From(email), DisplayName.From("Ada Lovelace"), Role.Employee);

    private async Task PersistAsync(User user)
    {
        await using var context = fixture.CreateContext();
        await new UserRepository(context).AddAsync(user, TestContext.Current.CancellationToken);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Round_trips_a_provisioning_user()
    {
        var user = RegisterEmployee("provisioning@example.com");
        await PersistAsync(user);

        await using var context = fixture.CreateContext();
        var found = await new UserRepository(context)
            .GetByIdentifierAsync(user.Identifier, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.Identifier.ShouldBe(user.Identifier);
        found.Value.Email.ShouldBe(user.Email);
        found.Value.DisplayName.ShouldBe(user.DisplayName);
        found.Value.Role.IsAdministrator.ShouldBeFalse();
        found.Value.Status.ShouldBe(UserStatus.Provisioning);
        found.Value.KeycloakSubjectIdentifier.ShouldBeNull();
    }

    [Fact]
    public async Task Round_trips_an_activated_administrator()
    {
        var user = User.Register(
            Email.From("admin@example.com"),
            DisplayName.From("Grace Hopper"),
            Role.Employee.GrantAdministrator());
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());
        user.Activate(subject);
        await PersistAsync(user);

        await using var context = fixture.CreateContext();
        var found = await new UserRepository(context)
            .GetByIdentifierAsync(user.Identifier, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value.Status.ShouldBe(UserStatus.Active);
        found.Value.Role.IsAdministrator.ShouldBeTrue();
        found.Value.KeycloakSubjectIdentifier.ShouldBe(subject);
    }

    [Fact]
    public async Task GetByIdentifier_returns_NotFound_for_an_unknown_identifier()
    {
        await using var context = fixture.CreateContext();
        var found = await new UserRepository(context)
            .GetByIdentifierAsync(UserIdentifier.New(), TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
        found.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task ExistsByEmail_reflects_whether_the_email_is_taken()
    {
        var user = RegisterEmployee("taken@example.com");
        await PersistAsync(user);

        await using var context = fixture.CreateContext();
        var users = new UserRepository(context);

        (await users.ExistsByEmailAsync(Email.From("taken@example.com"), TestContext.Current.CancellationToken))
            .ShouldBeTrue();
        (await users.ExistsByEmailAsync(Email.From("absent@example.com"), TestContext.Current.CancellationToken))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Rejects_a_second_account_with_the_same_email()
    {
        await PersistAsync(RegisterEmployee("duplicate@example.com"));

        await Should.ThrowAsync<DbUpdateException>(() => PersistAsync(RegisterEmployee("duplicate@example.com")));
    }

    [Fact]
    public async Task Rejects_two_accounts_linked_to_the_same_keycloak_subject()
    {
        var subject = KeycloakSubjectIdentifier.From(Guid.NewGuid());

        var first = RegisterEmployee("subject-one@example.com");
        first.Activate(subject);
        await PersistAsync(first);

        var second = RegisterEmployee("subject-two@example.com");
        second.Activate(subject);

        await Should.ThrowAsync<DbUpdateException>(() => PersistAsync(second));
    }

    [Fact]
    public async Task Allows_many_provisioning_accounts_without_a_keycloak_subject()
    {
        await PersistAsync(RegisterEmployee("no-subject-one@example.com"));

        await Should.NotThrowAsync(() => PersistAsync(RegisterEmployee("no-subject-two@example.com")));
    }
}
