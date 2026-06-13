using Shouldly;
using SmartSolutionsLab.Roomy.SharedKernel.Results;
using SmartSolutionsLab.Roomy.SharedKernel.Search;

namespace SmartSolutionsLab.Roomy.SharedKernel.Tests.Search;

public class SearchTermTests
{
    [Fact]
    public void An_omitted_term_is_the_empty_no_filter_term()
    {
        var term = SearchTerm.From(null);

        term.IsEmpty.ShouldBeTrue();
        term.Value.ShouldBeEmpty();
    }

    [Fact]
    public void A_whitespace_only_term_is_the_empty_no_filter_term()
    {
        SearchTerm.From("   ").IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void A_term_is_trimmed_of_surrounding_whitespace()
    {
        var term = SearchTerm.From("  Hannah  ");

        term.IsEmpty.ShouldBeFalse();
        term.Value.ShouldBe("Hannah");
    }

    [Fact]
    public void A_term_at_the_maximum_length_is_accepted()
    {
        var maxTerm = new string('a', SearchTerm.MaxLength);

        SearchTerm.From(maxTerm).Value.ShouldBe(maxTerm);
    }

    [Fact]
    public void A_term_longer_than_the_maximum_throws_a_bad_request()
    {
        var overLong = new string('a', SearchTerm.MaxLength + 1);

        var exception = Should.Throw<BadRequestException>(() => SearchTerm.From(overLong));

        exception.Error.Code.ShouldBe("search.term_too_long");
        exception.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public void Length_is_measured_after_trimming()
    {
        var paddedToOverLength = $"  {new string('a', SearchTerm.MaxLength)}  ";

        SearchTerm.From(paddedToOverLength).Value.Length.ShouldBe(SearchTerm.MaxLength);
    }

    [Fact]
    public void The_shared_empty_instance_is_the_no_filter_term()
    {
        SearchTerm.None.IsEmpty.ShouldBeTrue();
    }
}
