using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

public sealed record PageRequest
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;

    private PageRequest(string? cursor, int limit)
    {
        Cursor = cursor;
        Limit = limit;
    }

    public string? Cursor { get; }

    public int Limit { get; }

    public static PageRequest From(string? cursor, int? limit)
    {
        var effectiveLimit = limit ?? DefaultLimit;
        if (effectiveLimit < 1 || effectiveLimit > MaxLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), $"The page limit must be between 1 and {MaxLimit}.");
        }

        var trimmedCursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor;
        return new PageRequest(trimmedCursor, effectiveLimit);
    }

    public Result<TCursor?> DecodeCursor<TCursor>()
        where TCursor : class
    {
        if (Cursor is null) return Result.Success<TCursor?>(null);
        if (CursorCodec.TryDecode<TCursor>(Cursor, out var decoded)) return Result.Success<TCursor?>(decoded);

        return Error.Validation("pagination.cursor_invalid", "The pagination cursor is malformed.");
    }
}
