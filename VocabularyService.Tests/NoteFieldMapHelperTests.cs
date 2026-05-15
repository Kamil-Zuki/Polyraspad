using FluentAssertions;
using VocabularyService.Helpers;
using Xunit;

namespace VocabularyService.Tests;

public class NoteFieldMapHelperTests
{
    [Fact]
    public void CalculateTargetIndex_matches_whole_word_case_insensitively()
    {
        var expr = "How to Apply These Ideas to Business";
        var idx = NoteFieldMapHelper.CalculateTargetIndex(expr, "ideas");
        idx.Start.Should().Be(expr.IndexOf("Ideas", StringComparison.Ordinal));
        idx.Len.Should().Be(5);
        expr.Substring(idx.Start, idx.Len).Should().Be("Ideas");
    }

    [Fact]
    public void CalculateTargetIndex_does_not_match_suffix_inside_larger_word()
    {
        var expr = "How to Apply These Ideas to Business";
        var act = () => NoteFieldMapHelper.CalculateTargetIndex(expr, "deas");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("take off", "We take off now.")]
    [InlineData("well-known", "A well-known fact.")]
    public void CalculateTargetIndex_handles_phrases_and_hyphens(string word, string expr)
    {
        var idx = NoteFieldMapHelper.CalculateTargetIndex(expr, word);
        expr.Substring(idx.Start, idx.Len).Should().Be(word);
    }
}
