namespace SmartSolutionsLab.Roomy.TestSupport;

public static class EmployeeNameSamples
{
    public const string TypoTarget = "Hannah Schmidt";
    public const string TypoQuery = "Hanah";

    public const string LooserTypoMatch = "Hans Schmidt";

    // An accented name and the accent-stripped, lower-cased fragments that must still find it (FR-002): the
    // index and query both fold accents through immutable_unaccent.
    public const string AccentedTarget = "José Müller";
    public const string AccentStrippedGivenNameQuery = "jose";
    public const string AccentStrippedSurnameQuery = "muller";

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
