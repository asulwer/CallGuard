namespace CallGuard.Tests;

public class ContactLookupTests
{
    [Theory]
    [InlineData("com.google", "Google")]
    [InlineData("com.whatsapp", "WhatsApp")]
    [InlineData("org.thoughtcrime.securesms", "Signal")]
    [InlineData("org.telegram.messenger", "Telegram")]
    [InlineData(null, "this phone")]
    [InlineData("", "this phone")]
    [InlineData("vnd.sec.contact.phone", "this phone")]
    [InlineData("com.android.localphone", "this phone")]
    [InlineData("com.android.sim", "SIM card")]
    [InlineData("vnd.sec.contact.sim", "SIM card")]
    public void SourceName_Known(string? accountType, string expected) =>
        Assert.Equal(expected, ContactLookup.SourceName(accountType));

    [Fact]
    public void SourceName_Unknown_IsNull() =>
        Assert.Null(ContactLookup.SourceName("com.example.someapp"));

    [Fact]
    public void Contact_ListsEachSourceOnce() =>
        Assert.Equal(["Google", "WhatsApp"], ContactLookup.Contact("Google", "WhatsApp", "Google").Sources);
}
