using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.Infrastructure.Persistence.EfCore;

public static class NotFoundQueryableExtensions
{
    public static async Task<Result<T>> SingleOrNotFoundAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        Error notFound,
        CancellationToken cancellationToken)
        where T : class
    {
        var entity = await source.SingleOrDefaultAsync(predicate, cancellationToken);

        if (entity is null) return notFound;

        return entity;
    }
}
