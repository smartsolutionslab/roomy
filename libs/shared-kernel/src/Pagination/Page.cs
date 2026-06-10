namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

// One slice of a keyset-paginated list (ADR-0044): the items in their stable sort order plus an
// opaque NextCursor that locates the slice after this one — null when the list is exhausted.
public sealed record Page<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public static Page<T> Empty { get; } = new([], null);

    // Builds a page from a keyset probe — the limit + 1 rows a read model fetches to learn whether a
    // further page exists (ADR-0044). When the probe over-reads, the extra row is dropped and the last
    // kept row's sort key becomes the next cursor; otherwise the list is exhausted and the cursor is null.
    // toItem projects each kept row to its view; toCursorKey maps the last kept row to the cursor's
    // sort-key tuple. This is the probe-and-slice every keyset read model would otherwise repeat.
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
