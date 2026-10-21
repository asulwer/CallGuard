using Android.App;
using Android.Content;
using Android.Telecom;

namespace CallGuard;

/// <summary>
/// Android calls this for each call once the app holds the "Caller ID &amp; spam" role.
/// The decision itself is made by <see cref="ScreeningDecider"/>. Blocked calls are rejected: the phone
/// stays silent, the caller goes to voicemail, and the call still shows in call history.
/// Any problem — an error, or no decision in time (<see cref="FailOpen"/>) — lets the call ring.
/// </summary>
[Service(Name = "com.callguard.BlockerService", Exported = true,
         Permission = "android.permission.BIND_SCREENING_SERVICE")]
[IntentFilter(new[] { "android.telecom.CallScreeningService" })]
public class BlockerService : CallScreeningService
{
    const int DirectionIncoming = 0;    // Call.Details.DIRECTION_INCOMING
    const int PresentationAllowed = 1;  // TelecomManager.PRESENTATION_ALLOWED

    public override void OnScreenCall(Call.Details callDetails)
    {
        var started = Android.OS.SystemClock.ElapsedRealtime();
        FailOpen.Screen(() => Decide(callDetails), FailOpen.DefaultTimeLimit, outcome => Respond(callDetails, outcome, started));
    }

    /// <summary>Runs once per call, on a background thread.</summary>
    void Respond(Call.Details callDetails, ScreeningOutcome outcome, long started)
    {
        try
        {
            RespondToCall(callDetails, BuildResponse(outcome.Decision));
            // How long answering took, and how long the process had been running when Android asked.
            Android.Util.Log.Info("CallGuard", $"Answered Android in {Android.OS.SystemClock.ElapsedRealtime() - started} ms " +
                                               $"(process age {started - Android.OS.Process.StartElapsedRealtime} ms)");
        }
        catch (Exception ex)
        {
            // Android lets the call ring when the screening app doesn't answer.
            Android.Util.Log.Error("CallGuard", $"Could not answer Android, call allowed: {ex}");
        }

        try
        {
            if (outcome.Problem != null)
            {
                Android.Util.Log.Error("CallGuard", $"Screening failed, call allowed: {outcome.Problem}");
                Store.CallLog.AddError(outcome.Problem, RuleEngine.Normalize(callDetails.GetHandle()?.SchemeSpecificPart));
            }
            else if (outcome.Decision is { } decision)
            {
                Store.CallLog.Add(decision);
            }
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("CallGuard", $"Could not log call: {ex}");
        }
    }

    static CallResponse BuildResponse(ScreeningDecision? decision)
    {
        var builder = new CallResponse.Builder();
        switch (decision?.Action)
        {
            case ScreeningAction.Reject:
                builder.SetDisallowCall(true);
                builder.SetRejectCall(true);
                builder.SetSkipNotification(true);
                builder.SetSkipCallLog(false); // keep it in call history so you can see what was blocked
                break;
        }
        return builder.Build()!;
    }

    ScreeningDecision? Decide(Call.Details callDetails)
    {
        // Outgoing calls are also passed to screening apps; never interfere with those.
        if ((int)callDetails.CallDirection != DirectionIncoming) return null;

        var call = new IncomingCall(
            callDetails.GetHandle()?.SchemeSpecificPart,
            NumberHidden: (int)callDetails.HandlePresentation != PresentationAllowed);

        return ScreeningDecider.Decide(
            call,
            Store.PatternsForScreening,
            raw => ContactsChecker.Lookup(this, raw));
    }
}
