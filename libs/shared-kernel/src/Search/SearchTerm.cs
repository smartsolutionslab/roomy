using SmartSolutionsLab.Roomy.SharedKernel.Results;

namespace SmartSolutionsLab.Roomy.SharedKernel.Search;

public sealed record SearchTerm : IValueObject
{
    public const int MaxLength = 100;

    public static SearchTerm None { get; } = new(string.Empty);

    private SearchTerm(string value) => Value = value;

    public string Value { get; }

    public bool IsEmpty => Value.Length == 0;

    public static Result<SearchTerm> From(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return None;

        if (trimmed.Length > MaxLength)
        {
            return Error.Validation("search.term_too_long", $"The search term must be at most {MaxLength} characters.");
        }

        return new SearchTerm(trimmed);
    }

    public override string ToString() => Value;
}
