namespace CallGuard.Tests;

public class RuleEngineTests
{
    [Theory]
    [InlineData("6025551234", "6025551234")]
    [InlineData("+16025551234", "6025551234")]
    [InlineData("16025551234", "6025551234")]
    [InlineData("(602) 555-1234", "6025551234")]
    [InlineData("602.555.1234", "6025551234")]
    [InlineData("+442071234567", "+442071234567")]      // international numbers keep "+" and the country code
    [InlineData("+44 20 7123 4567", "+442071234567")]
    [InlineData("011442071234567", "+442071234567")]    // dialed the US way
    [InlineData("011 44 20 7123 4567", "+442071234567")]
    [InlineData("+1 876 555 1234", "8765551234")]       // +1 (incl. Caribbean) is the US zone
    [InlineData("+", "")]
    [InlineData("5551234", "5551234")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    [InlineData("anonymous", "")]
    [InlineData("٦٠٢٥٥٥١٢٣٤", "")] // non-ASCII digits are not phone digits
    public void Normalize(string? raw, string expected) =>
        Assert.Equal(expected, RuleEngine.Normalize(raw));

    [Theory]
    [InlineData("602*", "602*")]
    [InlineData("  602*  ", "602*")]
    [InlineData("(602) 555-****", "602555****")]
    [InlineData("602-555-????", "602555????")]
    [InlineData("+1 602*", "602*")]
    [InlineData("1602*", "602*")]
    [InlineData("602*  # spam callers", "602*")]
    [InlineData("602abc*", "602*")]
    [InlineData("*", "*")]
    [InlineData("+44*", "+44*")]              // a country code
    [InlineData("+44 20 ****", "+4420****")]
    [InlineData("011 44*", "+44*")]           // dialed the US way
    [InlineData("+234*  # Nigeria", "+234*")]
    [InlineData("44*", "44*")]                // no "+": still a US area-code pattern (440-449)
    public void CleanPattern_KeepsDigitsAndWildcards(string input, string expected) =>
        Assert.Equal(expected, RuleEngine.CleanPattern(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# just a comment")]
    [InlineData("abc")]
    [InlineData("1")] // only the country code
    [InlineData("+*")]   // a "country" with no country code
    [InlineData("011*")]
    public void CleanPattern_ReturnsNullWhenNothingIsLeft(string? input) =>
        Assert.Null(RuleEngine.CleanPattern(input));

    [Theory]
    [InlineData("6025551234", "602*")]
    [InlineData("6025551234", "602555*")]
    [InlineData("6025551234", "602555????")]
    [InlineData("6025551234", "602???1234")]
    [InlineData("6025551234", "6025551234")]
    [InlineData("6025551234", "*")]
    [InlineData("6025551234", "*1234")]
    [InlineData("+442071234567", "+44*")]
    [InlineData("+442071234567", "+4420*")]
    [InlineData("+2348012345678", "+234*")]
    public void FindMatch_Matches(string number, string pattern) =>
        Assert.Equal(pattern, RuleEngine.FindMatch(number, [pattern]));

    [Theory]
    [InlineData("4805551234", "602*")]
    [InlineData("6025551234", "603*")]
    [InlineData("6025551234", "602555???")]   // too short: ? is exactly one digit
    [InlineData("6025551234", "602555?????")] // too long
    [InlineData("6025551234", "602555")]      // no * means the whole number must match
    [InlineData("1602555123", "602*")]        // pattern is anchored at the start
    [InlineData("4405551234", "+44*")]        // a country pattern never matches a US number...
    [InlineData("+442071234567", "44*")]      // ...and a US pattern never matches an international one
    [InlineData("+442071234567", "440*")]
    [InlineData("+442071234567", "*")]        // "*" is for US numbers; international has its own setting
    [InlineData("+33123456789", "+44*")]
    public void FindMatch_DoesNotMatch(string number, string pattern) =>
        Assert.Null(RuleEngine.FindMatch(number, [pattern]));

    [Fact]
    public void FindMatch_ReturnsFirstMatchingPattern() =>
        Assert.Equal("602555*", RuleEngine.FindMatch("6025551234", ["480*", "602555*", "602*"]));

    [Fact]
    public void FindMatch_EmptyNumberNeverMatches() =>
        Assert.Null(RuleEngine.FindMatch("", ["*"]));

    [Fact]
    public void FindMatch_NoPatterns() =>
        Assert.Null(RuleEngine.FindMatch("6025551234", []));

    [Theory]
    [InlineData("*", true)]
    [InlineData("??????????", true)]
    [InlineData("?*", true)]
    [InlineData("602*", false)]
    [InlineData("*1234", false)]
    public void MatchesEveryNumber(string pattern, bool expected) =>
        Assert.Equal(expected, RuleEngine.MatchesEveryNumber(pattern));

    [Theory]
    [InlineData("6025551234", "(602) 555-1234")]
    [InlineData("5551234", "5551234")]
    [InlineData("+442071234567", "+442071234567")]
    public void Format(string number, string expected) =>
        Assert.Equal(expected, RuleEngine.Format(number));
}
