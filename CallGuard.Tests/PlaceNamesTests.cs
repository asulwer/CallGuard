namespace CallGuard.Tests;

public class PlaceNamesTests
{
    [Theory]
    [InlineData("602*", "Arizona")]
    [InlineData("602555*", "Arizona")]
    [InlineData("602???1234", "Arizona")]
    [InlineData("6025550123", "Arizona")]
    [InlineData("416*", "Ontario, Canada")]
    [InlineData("876*", "Jamaica")]
    [InlineData("809*", "Dominican Republic")]
    [InlineData("+44*", "United Kingdom")]
    [InlineData("+4420*", "London, United Kingdom")]
    [InlineData("+44161*", "Manchester, United Kingdom")]
    [InlineData("+234*", "Nigeria")]
    [InlineData("+91*", "India")]
    public void ForPattern(string pattern, string expected) =>
        Assert.Equal(expected, PlaceNames.ForPattern(pattern));

    [Theory]
    [InlineData("*")]
    [InlineData("?*")]
    [InlineData("60*")]       // not a whole area code yet
    [InlineData("555*")]      // not a real area code
    [InlineData("+999*")]     // not a real country code
    [InlineData("+*")]
    public void ForPattern_UnknownWhenItCantTell(string pattern) =>
        Assert.Null(PlaceNames.ForPattern(pattern));

    [Theory]
    [InlineData("6025550100", "Arizona")]
    [InlineData("4165550100", "Ontario, Canada")]
    [InlineData("+442071234567", "London, United Kingdom")]
    [InlineData("+2348012345678", "Nigeria")]
    public void ForNumber(string number, string expected) =>
        Assert.Equal(expected, PlaceNames.ForNumber(number));

    [Theory]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("5551234")]   // too short to have an area code
    public void ForNumber_UnknownWhenItCantTell(string number) =>
        Assert.Null(PlaceNames.ForNumber(number));
}
