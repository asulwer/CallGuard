using Android;
using Android.Content;
using Android.Content.PM;
using Android.Provider;

namespace CallGuard;

public static class ContactsChecker
{
    public static bool HasAccess(Context ctx) =>
        ctx.CheckSelfPermission(Manifest.Permission.ReadContacts) == Permission.Granted;

    /// <summary>
    /// True if the number belongs to a saved contact, false if not,
    /// null if that can't be told (no contacts access, or the contacts provider didn't answer).
    /// PhoneLookup matches loosely, so "+16025551234" finds a contact saved as "(602) 555-1234".
    /// </summary>
    public static bool? IsContact(Context ctx, string rawNumber)
    {
        if (!HasAccess(ctx)) return null;

        var uri = Android.Net.Uri.WithAppendedPath(
            ContactsContract.PhoneLookup.ContentFilterUri,
            Android.Net.Uri.Encode(rawNumber));

        using var cursor = ctx.ContentResolver?.Query(uri!, new[] { "_id" }, null, null, null);
        // No cursor means the lookup itself failed (e.g. contacts not ready right after boot) — that's
        // "unknown", not "not a contact", so the call isn't blocked because of it.
        return cursor == null ? null : cursor.Count > 0;
    }
}
