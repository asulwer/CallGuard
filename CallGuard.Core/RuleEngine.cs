using System.Text;
using System.Text.RegularExpressions;

namespace CallGuard;

/// <summary>
/// Turns phone numbers and user-entered patterns into a comparable form and matches them.
///
/// Numbers in the +1 zone (US, Canada, parts of the Caribbean) are compared as 10 digits, without the
/// leading 1. Other numbers keep a leading "+" and their country code ("+442071234567"), so they can never
/// be mistaken for a US number.
/// Pattern syntax:
///   *  = any number of digits (including none)
///   ?  = exactly one digit
///   +  = at the start, followed by a country code other than 1: matches only international numbers.
///        "011" (the US way to dial abroad) means the same.
///   Spaces, dashes, dots and parentheses are ignored, so "(602) 555-****" works. A leading "+1" or "1" is
///   the US country code and is dropped. Anything after # is a comment.
/// Examples:  602*        every number in area code 602
///            602555*     every number starting 602-555
///            602555????  same thing, written with single-digit wildcards
///            +44*        every number in the UK (country code 44), whatever comes after it
/// </summary>
public static class RuleEngine
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw.Trim();
        var digits = new string(s.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0) return "";

        if (s.StartsWith('+') && digits[0] != '1') return "+" + digits;
        if (!s.StartsWith('+') && digits.StartsWith("011") && digits.Length > 3) return "+" + digits[3..];

        if (digits.Length == 11 && digits[0] == '1') digits = digits[1..];
        return digits;
    }

    public static string? CleanPattern(string? line)
    {
        if (line == null) return null;
        var hash = line.IndexOf('#');
        if (hash >= 0) line = line[..hash];
        line = line.Trim();

        var sb = new StringBuilder();
        foreach (var c in line)
        {
            if (char.IsAsciiDigit(c) || c == '*' || c == '?') sb.Append(c);
        }
        var body = sb.ToString();

        // A country code other than 1, written as "+44..." or "011 44...": an international pattern.
        string? country = line.StartsWith('+') && body.Length > 0 && body[0] != '1' ? body
            : !line.StartsWith('+') && body.StartsWith("011") ? body[3..]
            : null;
        if (country != null)
            return country.Any(char.IsAsciiDigit) ? "+" + country : null;

        // US area codes never start with 1, so a leading 1 is the country code ("+1 602*").
        if (body.StartsWith('1')) body = body[1..];
        return body.Length == 0 ? null : body;
    }

    /// <summary>Returns the first pattern the number matches, or null if none match.</summary>
    public static string? FindMatch(string normalizedNumber, IEnumerable<string> patterns)
    {
        if (normalizedNumber.Length == 0) return null;
        foreach (var p in patterns)
        {
            if (Regex.IsMatch(normalizedNumber, ToRegex(p))) return p;
        }
        return null;
    }

    /// <summary>True if the pattern has no digits (e.g. "*" or "??????????"), so it matches every number.</summary>
    public static bool MatchesEveryNumber(string pattern) => !pattern.Any(char.IsAsciiDigit);

    /// <summary>Formats a normalized 10-digit number as (602) 555-1234; anything else (e.g. +44...) is returned as is.</summary>
    public static string Format(string n) =>
        n.Length == 10 ? $"({n[..3]}) {n[3..6]}-{n[6..]}" : n;

    static string ToRegex(string pattern)
    {
        var sb = new StringBuilder("^");
        foreach (var c in pattern)
        {
            sb.Append(c switch
            {
                '*' => "[0-9]*",
                '?' => "[0-9]",
                '+' => @"\+",
                _ => c.ToString(),
            });
        }
        sb.Append('$');
        return sb.ToString();
    }
}
