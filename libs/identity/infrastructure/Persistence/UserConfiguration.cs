using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

// EF Core mapping for the User aggregate. The value objects round-trip through their underlying
// primitives via value converters (the identifiers reuse their implicit Guid conversions). Two
// invariants are enforced at the database: Email is globally unique (FR-009), and a Keycloak subject
// links to at most one account — both as unique indexes. The Keycloak subject is nullable while an
// account is Provisioning, so its unique index admits many NULLs (one per not-yet-activated account).
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string TableName = "Users";
    public const string EmailIndexName = "UX_Users_Email";
    public const string KeycloakSubjectIndexName = "UX_Users_KeycloakSubjectIdentifier";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(TableName);

        builder.HasKey(user => user.Identifier);

        builder.Property(user => user.Identifier)
            .HasConversion(identifier => identifier.Value, value => UserIdentifier.From(value))
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.From(value))
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasConversion(displayName => displayName.Value, value => DisplayName.From(value))
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("IsAdministrator")
            .HasConversion(
                role => role.IsAdministrator,
                isAdministrator => isAdministrator ? Role.Employee.GrantAdministrator() : Role.Employee)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(user => user.KeycloakSubjectIdentifier)
            .HasConversion(new ValueConverter<KeycloakSubjectIdentifier, Guid>(
                subject => subject.Value,
                value => KeycloakSubjectIdentifier.From(value)));

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName(EmailIndexName);

        builder.HasIndex(user => user.KeycloakSubjectIdentifier)
            .IsUnique()
            .HasDatabaseName(KeycloakSubjectIndexName);
    }
}
