namespace CallGuard.Data;

public enum PatternAction { Block, Allow }

/// <summary>One saved pattern, e.g. "602*". See <see cref="RuleEngine"/> for the syntax.</summary>
public class NumberPattern
{
    public int Id { get; set; }

    /// <summary>The cleaned pattern: digits, * and ? only, without a leading country code.</summary>
    public required string Pattern { get; set; }

    public PatternAction Action { get; set; }

    /// <summary>Unticked patterns are kept but ignored when screening.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Optional note, e.g. "spam callers".</summary>
    public string? Label { get; set; }

    public DateTime CreatedUtc { get; set; }
}

/// <summary>One screened call in the recent-calls log.</summary>
public class CallLogEntry
{
    public int Id { get; set; }
    public DateTime TimeUtc { get; set; }

    /// <summary>Normalized number, or empty when hidden.</summary>
    public required string Number { get; set; }

    public ScreeningAction Action { get; set; }
    public required string Reason { get; set; }

    public string Describe() => ScreeningDecision.Describe(Action, Number, Reason);
}
