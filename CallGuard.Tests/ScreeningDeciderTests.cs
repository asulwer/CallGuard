namespace CallGuard.Tests;

public class ScreeningDeciderTests
{
    static readonly PatternSet Block602 = new(Block: ["602*"], Allow: []);

    static ScreeningDecision Decide(IncomingCall call, bool? isContact, PatternSet? patterns = null) =>
        ScreeningDecider.Decide(call, () => patterns ?? Block602, _ => isContact);

    static ScreeningDecision Decide(string? number, bool? isContact) => Decide(new IncomingCall(number), isContact);

    // --- blocked patterns and contacts ---

    [Fact]
    public void MatchingNumber_NotAContact_IsRejected()
    {
        var d = Decide("+16025551234", isContact: false);
        Assert.Equal(ScreeningAction.Reject, d.Action);
        Assert.Equal("BLOCKED  (602) 555-1234  matches 602*", d.Describe());
    }

    [Fact]
    public void MatchingNumber_ThatIsAContact_IsNotBlocked()
    {
        var d = Decide("+16025551234", isContact: true);
        Assert.Equal(ScreeningAction.Allow, d.Action);
        Assert.Equal("allowed  (602) 555-1234  contact (matches 602*)", d.Describe());
    }

    [Fact]
    public void MatchingNumber_WithoutContactsAccess_IsAllowed()
    {
        var d = Decide("+16025551234", isContact: null);
        Assert.False(d.Blocks);
        Assert.Equal("allowed  (602) 555-1234  matches 602*, NO CONTACTS ACCESS", d.Describe());
    }

    [Fact]
    public void NonMatchingNumber_IsAllowed_WithoutLookingUpContacts()
    {
        bool lookedUp = false;
        var d = ScreeningDecider.Decide(new IncomingCall("+14805551234"), () => Block602, _ => { lookedUp = true; return false; });
        Assert.False(d.Blocks);
        Assert.Equal("allowed  (480) 555-1234", d.Describe());
        Assert.False(lookedUp);
    }

    [Fact]
    public void ContactLookup_GetsTheRawNumber()
    {
        string? looked = null;
        ScreeningDecider.Decide(new IncomingCall("+1 (602) 555-1234"), () => Block602, n => { looked = n; return false; });
        Assert.Equal("+1 (602) 555-1234", looked);
    }

    [Fact]
    public void NoPatterns_AllowsEverything() =>
        Assert.False(Decide(new IncomingCall("+16025551234"), isContact: false, PatternSet.Empty).Blocks);

    // --- always-allow patterns ---

    [Fact]
    public void AlwaysAllowPattern_BeatsABlockedPattern()
    {
        var patterns = new PatternSet(Block: ["602*"], Allow: ["6025550123"]);
        var d = Decide(new IncomingCall("+16025550123"), isContact: false, patterns);
        Assert.Equal(ScreeningAction.Allow, d.Action);
        Assert.Equal("always allow 6025550123", d.Reason);
    }

    [Fact]
    public void AlwaysAllowPattern_BeatsACountryPattern()
    {
        var patterns = new PatternSet(Block: ["+44*"], Allow: ["+442071234567"]);
        var d = Decide(new IncomingCall("+442071234567"), isContact: false, patterns);
        Assert.False(d.Blocks);
        Assert.Equal("always allow +442071234567", d.Reason);
    }

    [Fact]
    public void AlwaysAllowPattern_OnlyCoversNumbersItMatches()
    {
        var patterns = new PatternSet(Block: ["602*"], Allow: ["6025550123"]);
        Assert.True(Decide(new IncomingCall("+16025550124"), isContact: false, patterns).Blocks);
    }

    // --- country patterns ---

    [Fact]
    public void InternationalNumber_WithoutAMatchingPattern_Rings() =>
        Assert.False(Decide(new IncomingCall("+442071234567"), isContact: false).Blocks);

    [Fact]
    public void CountryPattern_BlocksThatCountry()
    {
        var patterns = new PatternSet(Block: ["+44*"], Allow: []);
        var d = Decide(new IncomingCall("+44 20 7123 4567"), isContact: false, patterns);
        Assert.Equal(ScreeningAction.Reject, d.Action);
        Assert.Equal("BLOCKED  +442071234567  matches +44*", d.Describe());
    }

    [Theory]
    [InlineData("+33123456789")]   // another country
    [InlineData("+14405551234")]   // US area code 440 — shares its first digits with the UK's 44
    public void CountryPattern_LeavesOtherNumbersAlone(string number) =>
        Assert.False(Decide(new IncomingCall(number), isContact: false, new PatternSet(Block: ["+44*"], Allow: [])).Blocks);

    [Theory]
    [InlineData("+44 20 7123 4567", true)]    // London (area code 20)
    [InlineData("+44 161 123 4567", false)]   // Manchester
    public void RegionPattern_BlocksOnlyThatRegionOfTheCountry(string number, bool blocked) =>
        Assert.Equal(blocked, Decide(new IncomingCall(number), isContact: false, new PatternSet(Block: ["+4420*"], Allow: [])).Blocks);

    [Fact]
    public void CountryPattern_FromAContact_IsAllowed() =>
        Assert.False(Decide(new IncomingCall("+442071234567"), isContact: true, new PatternSet(Block: ["+44*"], Allow: [])).Blocks);

    // --- hidden numbers ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("anonymous")]
    public void HiddenNumber_IsAllowed(string? number)
    {
        var d = Decide(number, isContact: false);
        Assert.False(d.Blocks);
        Assert.Equal("allowed  hidden number", d.Describe());
    }

    [Fact]
    public void RestrictedPresentation_CountsAsHidden_EvenIfANumberIsPresent()
    {
        // The number would match 602*, but a withheld number is never matched against patterns.
        var d = Decide(new IncomingCall("+16025551234", NumberHidden: true), isContact: false);
        Assert.False(d.Blocks);
        Assert.Equal("", d.Number);
        Assert.Equal("allowed  hidden number", d.Describe());
    }
}
