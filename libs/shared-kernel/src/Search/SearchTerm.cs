using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Search;

// A bounded, optional free-text search term shared by every searchable list (012, ADR-0047). From trims
// the input, treats blank/whitespace as "no filter" (IsEmpty — the list returns its unfiltered keyset
// order), and rejects anything past the name length bound with a validation error, so a search never runs
// on pathological, unbounded input (FR-005). Accent/case folding is the read model's concern (it happens in
// SQL against the trigram index), so the term itself is only trimmed and length-bounded here.
public sealed record SearchTerm : IValueObject
{
    public const int MaxLength = 100;

    public static SearchTerm None { get; } = new(string.Empty);

    private SearchTerm(string value) => Value = value;

    // The normalized term — trimmed, and empty when the caller asked for no filter.
    public string Value { get; }

    public bool IsEmpty => Value.Length == 0;

    public static Result<SearchTerm> From(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return None;
        }

        if (trimmed.Length > MaxLength)
        {
            return Error.Validation(
                "search.term_too_long",
                $"The search term must be at most {MaxLength} characters.");
        }

        return new SearchTerm(trimmed);
    }

    public override string ToString() => Value;
}
