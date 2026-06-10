namespace SmartSolutionsLab.Roomy.TestSupport;

// A clock pinned to a fixed instant, so tests that depend on "now" (today's date, event timestamps) are
// deterministic. Replaces the live TimeProvider in the host under test.
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
