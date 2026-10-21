namespace CallGuard;

/// <param name="Decision">What to do with the call, or null to let it ring (also when blocking is off).</param>
/// <param name="Problem">Why no decision could be made, or null if screening worked.</param>
public sealed record ScreeningOutcome(ScreeningDecision? Decision, string? Problem);

/// <summary>
/// Runs a screening decision so that a problem can never block a call: if deciding throws or takes
/// longer than the time limit, the call is let through. Android gives screening apps about 5 seconds
/// in total (including starting the app), so the limit leaves room to answer before that.
/// </summary>
public static class FailOpen
{
    public static readonly TimeSpan DefaultTimeLimit = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Decides on a background thread and calls <paramref name="respond"/> exactly once: with the decision,
    /// or with a null decision (let it ring) and the problem. A decision that arrives too late is dropped.
    /// </summary>
    /// <returns>Completes after <paramref name="respond"/> has run.</returns>
    public static Task Screen(Func<ScreeningDecision?> decide, TimeSpan timeLimit, Action<ScreeningOutcome> respond)
    {
        var work = Task.Run(decide);
        return Task.WhenAny(work, Task.Delay(timeLimit)).ContinueWith(first =>
        {
            if (first.Result != work)
                respond(new ScreeningOutcome(null, $"no decision within {timeLimit.TotalSeconds:0.#} s"));
            else if (work.IsCompletedSuccessfully)
                respond(new ScreeningOutcome(work.Result, null));
            else
                respond(new ScreeningOutcome(null, work.Exception?.GetBaseException().Message ?? "screening failed"));
        }, TaskScheduler.Default);
    }
}
