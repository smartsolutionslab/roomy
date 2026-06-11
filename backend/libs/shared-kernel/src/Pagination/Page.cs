namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public static Page<T> Empty { get; } = new([], null);

    public static Page<T> FromProbe<TRow, TCursor>(
        IReadOnlyList<TRow> probeRows,
        int limit,
        Func<TRow, T> toItem,
        Func<TRow, TCursor> toCursorKey)
    {
        var hasMore = probeRows.Count > limit;
        var pageRows = hasMore ? probeRows.Take(limit).ToList() : probeRows;
        var items = pageRows.Select(toItem).ToList();
        var nextCursor = hasMore ? CursorCodec.Encode(toCursorKey(pageRows[^1])) : null;
        return new Page<T>(items, nextCursor);
    }
}
