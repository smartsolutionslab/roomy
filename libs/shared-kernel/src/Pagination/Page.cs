namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

// One slice of a keyset-paginated list (ADR-0044): the items in their stable sort order plus an
// opaque NextCursor that locates the slice after this one — null when the list is exhausted.
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public static Page<T> Empty { get; } = new([], null);
}
