using CallGuard.Data;

namespace CallGuard.Tests;

public class PatternFileTests
{
    [Theory]
    [InlineData("block 602*", PatternAction.Block, "602*", null)]
    [InlineData("allow 6025550123", PatternAction.Allow, "6025550123", null)]
    [InlineData("ALLOW 6025550123", PatternAction.Allow, "6025550123", null)]
    [InlineData("allow: 480*", PatternAction.Allow, "480*", null)]
    [InlineData("602*", PatternAction.Block, "602*", null)]                       // no keyword means block
    [InlineData("block (602) 555-****  # Phoenix spam", PatternAction.Block, "602555****", "Phoenix spam")]
    [InlineData("  allow +1 602 555 0123 #doctor  ", PatternAction.Allow, "6025550123", "doctor")]
    [InlineData("block 602* #", PatternAction.Block, "602*", null)]
    [InlineData("block +44*  # UK", PatternAction.Block, "+44*", "UK")]
    [InlineData("allow +442071234567", PatternAction.Allow, "+442071234567", null)]
    public void Parse_ReadsALine(string line, PatternAction action, string pattern, string? label)
    {
        var p = Assert.Single(PatternFile.Parse(line));
        Assert.Equal(action, p.Action);
        Assert.Equal(pattern, p.Pattern);
        Assert.Equal(label, p.Label);
        Assert.True(p.Enabled);
    }

    [Theory]
    [InlineData("block 480* (off)")]
    [InlineData("block 480* (OFF)  # old rule")]
    [InlineData("(off) block 480*")]
    public void Parse_OffMarker_SwitchesThePatternOff(string line)
    {
        var p = Assert.Single(PatternFile.Parse(line));
        Assert.Equal("480*", p.Pattern);
        Assert.False(p.Enabled);
    }

    [Fact]
    public void Parse_SkipsBlankAndCommentLines_AndKeepsLineNumbers()
    {
        var lines = PatternFile.Parse("# header\n\n   \nblock 602*\r\n# another\nallow 480*\n");
        Assert.Equal([4, 6], lines.Select(l => l.LineNumber));
    }

    [Theory]
    [InlineData("block")]
    [InlineData("allow   # just a note")]
    [InlineData("hello")]
    public void Parse_LineWithoutAPattern_HasNullPattern(string line) =>
        Assert.Null(Assert.Single(PatternFile.Parse(line)).Pattern);

    [Fact]
    public void Write_ProducesReadableLines()
    {
        var text = PatternFile.Write(
        [
            new NumberPattern { Pattern = "602*", Action = PatternAction.Block, Label = "Phoenix spam" },
            new NumberPattern { Pattern = "6025550123", Action = PatternAction.Allow },
            new NumberPattern { Pattern = "480*", Action = PatternAction.Block, IsEnabled = false },
        ]);

        Assert.Contains("block 602*  # Phoenix spam\n", text);
        Assert.Contains("allow 6025550123\n", text);
        Assert.Contains("block 480* (off)\n", text);
        Assert.Equal(3, PatternFile.Parse(text).Count); // header lines are comments
    }
}
