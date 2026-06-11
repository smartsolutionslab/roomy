using SmartSolutionsLab.Roomy.Identity.Application;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;
using SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Identity.Api.Seeding;

public sealed class DefaultAdminSeeder(
    IUserRepository users,
    IIdentityProviderPort identityProvider,
    IdentityDbContext dbContext,
    DefaultAdminOptions options)
{
    public async Task<Result> SeedAsync(CancellationToken cancellationToken)
    {
        var email = Email.From(options.Email);
        if (await users.ExistsByEmailAsync(email, cancellationToken)) return Result.Success();

        var displayName = DisplayName.From(options.DisplayName);
        var administrator = Role.Employee.GrantAdministrator();

        var provisioning = await identityProvider.ProvisionUserAsync(
            email,
            displayName,
            options.InitialPassword,
            administrator,
            cancellationToken);
        if (provisioning.IsFailure) return provisioning.Error;

        var admin = User.Register(email, displayName, administrator);
        admin.Activate(provisioning.Value);

        await users.AddAsync(admin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
