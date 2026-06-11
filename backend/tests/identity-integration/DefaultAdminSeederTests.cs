using Microsoft.EntityFrameworkCore;
using Shouldly;
using SmartSolutionsLab.Roomy.Identity.Api.Seeding;
using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.IntegrationTests;

public sealed class DefaultAdminSeederTests(PostgresDatabaseFixture fixture)
    : IClassFixture<PostgresDatabaseFixture>
{
    private static DefaultAdminOptions OptionsFor(string email) =>
        new() { Email = email, DisplayName = "Default Admin", InitialPassword = "default-admin-password" };

    private sealed class RecordingIdentityProvider : IIdentityProviderPort
    {
        public int Calls { get; private set; }
        public Role LastRole { get; private set; }

        public Task<Result<KeycloakSubjectIdentifier>> ProvisionUserAsync(
            Email email,
            DisplayName displayName,
            string initialPassword,
            Role role,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRole = role;
            return Task.FromResult(Result.Success(KeycloakSubjectIdentifier.From(Guid.NewGuid())));
        }

        public Task<Result> AssignAdministratorRoleAsync(
            KeycloakSubjectIdentifier subject,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Seeding does not elevate an existing account.");
    }

    [Fact]
    public async Task Seeds_the_default_admin_as_an_active_administrator()
    {
        var provider = new RecordingIdentityProvider();
        var options = OptionsFor("admin-seed@roomy.test");

        await using (var context = fixture.CreateContext())
        {
            var result = await new DefaultAdminSeeder(new UserRepository(context), provider, context, options)
                .SeedAsync(TestContext.Current.CancellationToken);
            result.IsSuccess.ShouldBeTrue();
        }

        provider.Calls.ShouldBe(1);
        provider.LastRole.IsAdministrator.ShouldBeTrue();

        await using (var context = fixture.CreateContext())
        {
            var admin = await context.Users.SingleAsync(
                user => user.Email == Email.From("admin-seed@roomy.test"),
                TestContext.Current.CancellationToken);

            admin.IsAdministrator.ShouldBeTrue();
            admin.Status.ShouldBe(UserStatus.Active);
            admin.KeycloakSubjectIdentifier.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task Is_idempotent_across_restarts()
    {
        var provider = new RecordingIdentityProvider();
        var options = OptionsFor("admin-idempotent@roomy.test");

        await using (var context = fixture.CreateContext())
        {
            await new DefaultAdminSeeder(new UserRepository(context), provider, context, options)
                .SeedAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = fixture.CreateContext())
        {
            await new DefaultAdminSeeder(new UserRepository(context), provider, context, options)
                .SeedAsync(TestContext.Current.CancellationToken);
        }

        provider.Calls.ShouldBe(1);

        await using (var verification = fixture.CreateContext())
        {
            var count = await verification.Users.CountAsync(
                user => user.Email == Email.From("admin-idempotent@roomy.test"),
                TestContext.Current.CancellationToken);
            count.ShouldBe(1);
        }
    }
}
