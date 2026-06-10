namespace SmartSolutionsLab.Roomy.TestSupport;

// A context-agnostic corpus of employee display names for the employee-search tests (012): diverse,
// near-duplicate, typo-adjacent, and accented. It is deliberately just strings — each context's test maps a
// name onto its own row type (the attendance Employees read model / the organization Employee table), so this
// stays free of any context's entity types and is shared by both backend stories.
public static class EmployeeNameSamples
{
    // The intended target of the single-typo scenario (SC-002) and a query that misspells it by one deleted
    // letter ("Hannah" -> "Hanah"): the search must still rank the target on the first page.
    public const string TypoTarget = "Hannah Schmidt";
    public const string TypoQuery = "Hanah";

    // A name that is similar to the typo query but a looser match than the target, so a best-match-first order
    // is observable: "Hannah" matches "Hanah" more closely than "Hans" does.
    public const string LooserTypoMatch = "Hans Schmidt";

    // An accented name and the accent-stripped, lower-cased fragments that must still find it (FR-002): the
    // index and query both fold accents through immutable_unaccent.
    public const string AccentedTarget = "José Müller";
    public const string AccentStrippedGivenNameQuery = "jose";
    public const string AccentStrippedSurnameQuery = "muller";

    // A broad, realistic corpus. The Schmidt/Schneider cluster gives the typo and ranking cases their
    // near-duplicates; the accented names exercise folding; the rest are dissimilar enough to stay below the
    // similarity threshold for the typo query, so they do not crowd the intended match.
    public static IReadOnlyList<string> Corpus { get; } =
    [
        TypoTarget,
        LooserTypoMatch,
        "Hanna Schneider",
        "Johanna Schmitt",
        "Heinrich Schmid",
        AccentedTarget,
        "Renée Dubois",
        "Søren Jørgensen",
        "Ada Lovelace",
        "Alan Turing",
        "Grace Hopper",
        "Edsger Dijkstra",
        "Barbara Liskov",
        "Donald Knuth",
        "Margaret Hamilton",
        "Katherine Johnson",
        "Linus Torvalds",
        "Ken Thompson",
    ];
}
