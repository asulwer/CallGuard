using Android;
using Android.Accounts;
using Android.Content;
using Android.Content.PM;
using Android.Provider;

namespace CallGuard;

public static class ContactsChecker
{
    public static bool HasAccess(Context ctx) =>
        ctx.CheckSelfPermission(Manifest.Permission.ReadContacts) == Permission.Granted;

    /// <summary>
    /// Whether the number belongs to a saved contact (null if that can't be told: no contacts access, or the
    /// contacts provider didn't answer), and which accounts the copies holding it come from.
    /// PhoneLookup matches loosely, so "+16025551234" finds a contact saved as "(602) 555-1234".
    /// </summary>
    public static ContactLookup Lookup(Context ctx, string rawNumber)
    {
        if (!HasAccess(ctx)) return ContactLookup.Unknown;

        var uri = Android.Net.Uri.WithAppendedPath(
            ContactsContract.PhoneLookup.ContentFilterUri,
            Android.Net.Uri.Encode(rawNumber));

        var contactIds = new List<long>();
        using (var cursor = ctx.ContentResolver?.Query(uri!, new[] { "_id" }, null, null, null))
        {
            // No cursor means the lookup itself failed (e.g. contacts not ready right after boot) — that's
            // "unknown", not "not a contact", so the call isn't blocked because of it.
            if (cursor == null) return ContactLookup.Unknown;
            while (cursor.MoveToNext()) contactIds.Add(cursor.GetLong(0));
        }
        if (contactIds.Count == 0) return ContactLookup.NotAContact;

        // Where the contact comes from is only shown to the user; it never changes the answer.
        try
        {
            return ContactLookup.Contact(Sources(ctx, rawNumber, contactIds));
        }
        catch (Exception)
        {
            return ContactLookup.Contact();
        }
    }

    /// <summary>
    /// The accounts of these contacts' phone numbers — just the copies that hold this number, when any do
    /// exactly (PhoneLookup may have matched a differently written one).
    /// </summary>
    static IEnumerable<string> Sources(Context ctx, string rawNumber, List<long> contactIds)
    {
        var number = RuleEngine.Normalize(rawNumber);
        var holding = new List<string?>();
        var all = new List<string?>();

        using (var cursor = ctx.ContentResolver?.Query(
            ContactsContract.CommonDataKinds.Phone.ContentUri!,
            new[] { ContactsContract.CommonDataKinds.Phone.Number, "account_type" },
            $"contact_id IN ({string.Join(",", contactIds)})", null, null))
        {
            if (cursor == null) return [];
            while (cursor.MoveToNext())
            {
                var accountType = cursor.IsNull(1) ? null : cursor.GetString(1);
                all.Add(accountType);
                if (RuleEngine.Normalize(cursor.GetString(0)) == number) holding.Add(accountType);
            }
        }

        return (holding.Count > 0 ? holding : all).Distinct().Select(type => SourceName(ctx, type));
    }

    static string SourceName(Context ctx, string? accountType)
    {
        if (ContactLookup.SourceName(accountType) is { } known) return known;

        // An account type we don't know: name it after the app that owns it, e.g. "Microsoft Teams".
        try
        {
            var owner = AccountManager.Get(ctx)?.GetAuthenticatorTypes()?.FirstOrDefault(a => a.Type == accountType);
            var label = owner == null ? null : ctx.PackageManager?.GetText(owner.PackageName!, owner.LabelId, null);
            if (!string.IsNullOrWhiteSpace(label)) return label;
        }
        catch (Exception)
        {
        }
        return accountType!;
    }
}
