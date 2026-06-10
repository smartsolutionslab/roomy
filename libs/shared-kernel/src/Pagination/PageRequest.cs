using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Pagination;

// A validated request for one page of a keyset-paginated list (ADR-0044): an optional opaque cursor
// (absent = first page) and a limit defaulted to 50 and capped at 100. A limit outside [1, 100] is a
// validation error here; a malformed cursor is a validation error at DecodeCursor, where the list's
// sort-key type is known. The server never silently clamps — both surface as a 400.
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

    public static Result<PageRequest> From(string? cursor, int? limit)
    {
        var effectiveLimit = limit ?? DefaultLimit;
        if (effectiveLimit < 1 || effectiveLimit > MaxLimit)
        {
            return Error.Validation(
                "pagination.limit_out_of_range",
                $"The page limit must be between 1 and {MaxLimit}.");
        }

        var trimmedCursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor;
        return new PageRequest(trimmedCursor, effectiveLimit);
    }

    // Decodes the cursor as this list's sort-key tuple. Absent cursor → the first page (null key);
    // a present-but-unreadable cursor → a validation error (400). Read models call this with the
    // record that names their sort key.
    public Result<TCursor?> DecodeCursor<TCursor>()
        where TCursor : class
    {
        if (Cursor is null)
        {
            return Result.Success<TCursor?>(null);
        }

        if (CursorCodec.TryDecode<TCursor>(Cursor, out var decoded))
        {
            return Result.Success<TCursor?>(decoded);
        }

        return Error.Validation("pagination.cursor_invalid", "The pagination cursor is malformed.");
    }
}
