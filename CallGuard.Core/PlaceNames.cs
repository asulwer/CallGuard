using System.Globalization;
using PhoneNumbers;

namespace CallGuard;

/// <summary>
/// Where numbers and patterns are from — "Arizona", "Ontario, Canada", "Jamaica", "London, United Kingdom" —
/// using the offline geocoding data in Google's libphonenumber (no internet access). Loading that data takes
/// a moment, so it's only used on screen, never while screening a call.
/// </summary>
public static class PlaceNames
{
    static readonly Lazy<PhoneNumberUtil> Util = new(PhoneNumberUtil.GetInstance);
    static readonly Lazy<PhoneNumberOfflineGeocoder> Geocoder = new(PhoneNumberOfflineGeocoder.GetInstance);

    // The geocoder loads its data files on first use and isn't documented as thread-safe.
    static readonly Lock Gate = new();

    // Fills a US-zone prefix out to 10 digits. 555 isn't a real exchange, so an area code on its own
    // resolves to its state or country rather than to whichever town owns some real exchange.
    const string UsFiller = "5550100";

    /// <summary>Loads the data in advance, e.g. on a background thread, so the first screen that needs it doesn't wait.</summary>
    public static void WarmUp() => ForNumber("6025550100");

    /// <summary>For a number as Call Guard stores it: 10 US-zone digits, or "+" and a country code.</summary>
    public static string? ForNumber(string normalizedNumber)
    {
        if (normalizedNumber.StartsWith('+')) return normalizedNumber.Length > 1 ? Describe(normalizedNumber) : null;
        return normalizedNumber.Length == 10 ? Describe("+1" + normalizedNumber) : null;
    }

    /// <summary>
    /// For a pattern, from the digits before its first wildcard: "602*" is Arizona, "+44*" the United Kingdom,
    /// "+4420*" London. Null when there are too few digits to tell, e.g. "60*" or "*".
    /// </summary>
    public static string? ForPattern(string pattern)
    {
        var cut = pattern.IndexOfAny(['*', '?']);
        var prefix = cut < 0 ? pattern : pattern[..cut];

        if (prefix.StartsWith('+'))
        {
            var digits = prefix[1..];
            // Country codes are 1–3 digits and none is the start of another, so the first one that exists is it.
            for (int length = 1; length <= Math.Min(3, digits.Length); length++)
            {
                var region = Util.Value.GetRegionCodeForCountryCode(int.Parse(digits[..length]));
                if (region == "ZZ") continue;
                return digits.Length == length
                    ? CountryName(region)
                    : Describe("+" + digits.PadRight(length + 10, '0')) ?? CountryName(region);
            }
            return null;
        }

        // A US-zone pattern needs at least the 3-digit area code.
        if (prefix.Length < 3 || prefix.Length > 10) return null;
        return Describe("+1" + prefix + UsFiller[(prefix.Length - 3)..]);
    }

    static string? Describe(string e164)
    {
        lock (Gate)
            return DescribeLocked(e164);
    }

    static string? DescribeLocked(string e164)
    {
        PhoneNumber number;
        try
        {
            number = Util.Value.Parse(e164, null);
        }
        catch (NumberParseException)
        {
            return null;
        }

        var place = Geocoder.Value.GetDescriptionForValidNumber(number, Locale.English);
        var region = Util.Value.GetRegionCodeForNumber(number);
        if (region == null)
        {
            // Not a number that exists (e.g. area code 555): only the country is known, and for +1 not even that.
            if (number.CountryCode == 1) return null;
            region = Util.Value.GetRegionCodeForCountryCode(number.CountryCode);
        }

        if (region == "US") return place.Length > 0 ? place : CountryName(region);
        var country = CountryName(region);
        return place.Length == 0 || place == country ? country : $"{place}, {country}";
    }

    static string CountryName(string region)
    {
        try
        {
            return new RegionInfo(region).EnglishName;
        }
        catch (ArgumentException)
        {
            return region;
        }
    }
}
