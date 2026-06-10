using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.SharedKernel.Guards;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

public static class SnakeCaseNamingConvention
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null) entity.SetTableName(ToSnakeCase(tableName));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName is not null)
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (constraintName is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    /// <summary>
    /// Converts a PascalCase / camelCase identifier to snake_case (e.g. <c>StreamId</c> →
    /// <c>stream_id</c>). A separator is inserted at a lower→upper boundary and at the end of an
    /// uppercase run that is followed by a lowercase letter, so acronyms stay together
    /// (<c>UXEvents</c> → <c>ux_events</c>); existing underscores are preserved as-is.
    /// </summary>
    public static string ToSnakeCase(string name)
    {
        Ensure.That(name).IsNotEmpty();

        var builder = new StringBuilder(name.Length + 8);

        for (var index = 0; index < name.Length; index++)
        {
            var current = name[index];

            if (char.IsUpper(current) && index > 0 && name[index - 1] != '_' && NeedsSeparatorBefore(name, index))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    private static bool NeedsSeparatorBefore(string name, int index)
    {
        var previous = name[index - 1];

        if (!char.IsUpper(previous)) return true;

        // Upper → Upper only splits when the next char is lower, i.e. the end of an acronym run
        // beginning a new word (e.g. the "S" in "HTTPServer" → "http_server").
        var next = index + 1 < name.Length ? name[index + 1] : '\0';
        return char.IsLower(next);
    }
}
