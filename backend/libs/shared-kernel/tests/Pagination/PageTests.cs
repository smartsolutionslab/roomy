using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Pagination;

public class PageTests
{
    private sealed record Row(int Sort, string Label);

    private sealed record Cursor(int Sort);

    [Fact]
    public void FromProbe_returns_every_row_and_no_cursor_when_the_probe_did_not_over_read()
    {
        Row[] probe = [new(1, "a"), new(2, "b")];

        var page = Page<string>.FromProbe(probe, limit: 2, row => row.Label, row => new Cursor(row.Sort));

        page.Items.ShouldBe(["a", "b"]);
        page.NextCursor.ShouldBeNull();
    }

    [Fact]
    public void FromProbe_drops_the_extra_row_and_issues_a_cursor_when_the_probe_over_read()
    {
        Row[] probe = [new(1, "a"), new(2, "b"), new(3, "c")];

        var page = Page<string>.FromProbe(probe, limit: 2, row => row.Label, row => new Cursor(row.Sort));

        page.Items.ShouldBe(["a", "b"]);
        page.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public void FromProbe_builds_the_cursor_from_the_last_kept_row_not_the_dropped_one()
    {
        Row[] probe = [new(1, "a"), new(2, "b"), new(3, "c")];

        var page = Page<string>.FromProbe(probe, limit: 2, row => row.Label, row => new Cursor(row.Sort));

        CursorCodec.TryDecode<Cursor>(page.NextCursor!, out var cursor).ShouldBeTrue();
        cursor.Sort.ShouldBe(2);
    }

    [Fact]
    public void FromProbe_returns_an_empty_page_for_no_rows()
    {
        var page = Page<string>.FromProbe(Array.Empty<Row>(), limit: 2, row => row.Label, row => new Cursor(row.Sort));

        page.Items.ShouldBeEmpty();
        page.NextCursor.ShouldBeNull();
    }
}
