namespace CallGuard;

/// <param name="RawNumber">The caller's number as Android reports it.</param>
/// <param name="NumberHidden">True when the caller withheld their number (private, unknown, payphone).</param>
public sealed record IncomingCall(
    string? RawNumber,
    bool NumberHidden = false);

/// <summary>Allow lets the call ring; Reject declines it (the phone stays silent, the caller goes to voicemail).</summary>
public enum ScreeningAction { Allow, Reject }

/// <summary>The saved patterns, split by what they do.</summary>
public sealed record PatternSet(IReadOnlyList<string> Block, IReadOnlyList<string> Allow)
{
    public static readonly PatternSet Empty = new([], []);
}

/// <param name="Number">The normalized number, or empty when hidden.</param>
/// <param name="Reason">Why this action was taken, or empty when no rule applied.</param>
public readonly record struct ScreeningDecision(ScreeningAction Action, string Number, string Reason)
{
    public bool Blocks => Action != ScreeningAction.Allow;

    public string Describe() => Describe(Action, Number, Reason);

    public static string Verb(ScreeningAction action) => action switch
    {
        ScreeningAction.Reject => "BLOCKED",
        _ => "allowed",
    };

    /// <summary>One log line, e.g. "BLOCKED  (602) 555-1234  matches 602*".</summary>
    public static string Describe(ScreeningAction action, string number, string reason)
    {
        var parts = new List<string> { Verb(action) };
        if (number.Length > 0) parts.Add(RuleEngine.Format(number));
        if (reason.Length > 0) parts.Add(reason);
        return string.Join("  ", parts);
    }
}

/// <summary>
/// Decides what to do with an incoming call.
/// Decision order (only patterns that are switched on count):
///   1. Hidden / private number               -> allow (block those with the Phone app's "Unknown" setting)
///   2. Number matches an always-allow pattern -> allow
///   3. Number matches no blocked pattern      -> allow
///   4. Number is a saved contact             -> allow, even though it matches a blocked pattern
///   5. Otherwise                             -> reject
/// If contacts access is missing, calls that would be blocked are allowed rather than risk blocking a contact.
/// </summary>
public static class ScreeningDecider
{
    /// <param name="patterns">Loads the saved patterns; only called when needed.</param>
    /// <param name="isContact">
    /// Looks up the raw number in contacts: true if saved, false if not, null if contacts can't be read.
    /// Only called when there is a reason to block.
    /// </param>
    public static ScreeningDecision Decide(
        IncomingCall call,
        Func<PatternSet> patterns,
        Func<string, bool?> isContact)
    {
        var number = RuleEngine.Normalize(call.RawNumber);
        if (call.NumberHidden || number.Length == 0)
            return new(ScreeningAction.Allow, "", "hidden number");

        var set = patterns();
        if (RuleEngine.FindMatch(number, set.Allow) is { } allowedBy)
            return new(ScreeningAction.Allow, number, $"always allow {allowedBy}");

        if (RuleEngine.FindMatch(number, set.Block) is not { } match)
            return new(ScreeningAction.Allow, number, "");
        var reason = $"matches {match}";

        return isContact(call.RawNumber!) switch
        {
            null => new(ScreeningAction.Allow, number, $"{reason}, NO CONTACTS ACCESS"),
            true => new(ScreeningAction.Allow, number, $"contact ({reason})"),
            false => new(ScreeningAction.Reject, number, reason),
        };
    }
}
