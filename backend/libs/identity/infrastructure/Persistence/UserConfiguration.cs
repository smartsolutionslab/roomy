using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Roomy.Identity.Domain.Users;

namespace SmartSolutionsLab.Roomy.Identity.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const string TableName = "Users";
    public const string EmailIndexName = "UX_Users_Email";
    public const string KeycloakSubjectIndexName = "UX_Users_KeycloakSubjectIdentifier";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(TableName);

        builder.Ignore(user => user.DomainEvents);

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
                isAdministrator => Role.From(isAdministrator))
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
