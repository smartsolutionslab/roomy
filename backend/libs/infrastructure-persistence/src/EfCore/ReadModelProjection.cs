using Microsoft.EntityFrameworkCore;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

public static class ReadModelProjection
{
    // Find-or-create-or-update a read-model row by key, then save. `create` builds the row when it is
    // absent; `update` mutates the existing one. Extracts the find / branch / save shape that every
    // integration-event projection repeats.
    public static async Task UpsertAsync<TEntity>(
        this DbContext context,
        object key,
        Func<TEntity> create,
        Action<TEntity> update,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var existing = await context.FindAsync<TEntity>([key], cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            context.Add(create());
        }
        else
        {
            update(existing);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
