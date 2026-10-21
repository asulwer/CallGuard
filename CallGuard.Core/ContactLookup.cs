namespace CallGuard;

/// <summary>Whether a number is a saved contact, and where that contact comes from.</summary>
/// <param name="Found">True if saved, false if not, null if contacts can't be read.</param>
/// <param name="Sources">
/// Where the copies holding the number are stored, e.g. "Google", "WhatsApp", "this phone". A contact can be
/// made of several copies (Google account, SIM, messaging apps), and a forgotten copy with an old number still
/// makes that number a contact. Empty when unknown.
/// </param>
public sealed record ContactLookup(bool? Found, IReadOnlyList<string> Sources)
{
    public static readonly ContactLookup NotAContact = new(false, []);
    public static readonly ContactLookup Unknown = new(null, []);

    public static ContactLookup Contact(params IEnumerable<string> sources) => new(true, sources.Distinct().ToList());

    /// <summary>
    /// A friendly name for a contacts account type, e.g. "com.whatsapp" is WhatsApp. Null for one it doesn't
    /// know, which the caller can name another way (e.g. from the app that owns the account).
    /// </summary>
    public static string? SourceName(string? accountType)
    {
        if (string.IsNullOrEmpty(accountType)) return ThisPhone;
        if (KnownSources.TryGetValue(accountType, out var name)) return name;
        // Phone makers each name these differently: "com.android.sim", "vnd.sec.contact.sim", ...
        if (accountType.Contains("sim", StringComparison.OrdinalIgnoreCase)) return "SIM card";
        if (accountType.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
            accountType.Contains("local", StringComparison.OrdinalIgnoreCase)) return ThisPhone;
        return null;
    }

    /// <summary>Contacts saved on the phone only, not in any account.</summary>
    public const string ThisPhone = "this phone";

    static readonly Dictionary<string, string> KnownSources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["com.google"] = "Google",
        ["com.whatsapp"] = "WhatsApp",
        ["com.whatsapp.w4b"] = "WhatsApp Business",
        ["org.thoughtcrime.securesms"] = "Signal",
        ["org.telegram.messenger"] = "Telegram",
        ["com.viber.voip"] = "Viber",
        ["com.microsoft.office.outlook"] = "Outlook",
        ["com.android.exchange"] = "Exchange",
        ["com.google.android.gm.exchange"] = "Exchange",
        ["com.samsung.android.coreapps"] = "Samsung",
    };
}
