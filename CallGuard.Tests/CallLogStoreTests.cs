using CallGuard.Data;

namespace CallGuard.Tests;

public sealed class CallLogStoreTests : TempDatabase
{
    CallLogStore Log => Db.CallLog;

    static ScreeningDecision Blocked(string number = "6025551234") => new(ScreeningAction.Reject, number, "matches 602*");
    static ScreeningDecision Allowed(string number = "4805551234") => new(ScreeningAction.Allow, number, "");

    [Fact]
    public void Add_StoresTheDecision()
    {
        var time = new DateTime(2026, 10, 20, 18, 30, 0, DateTimeKind.Utc);
        Log.Add(Blocked(), time);

        var e = Assert.Single(Log.Recent());
        Assert.Equal("6025551234", e.Number);
        Assert.Equal(ScreeningAction.Reject, e.Action);
        Assert.Equal("matches 602*", e.Reason);
        Assert.Equal(time, e.TimeUtc);
        Assert.Equal("BLOCKED  (602) 555-1234  matches 602*", e.Describe());
    }

    [Fact]
    public void Recent_IsNewestFirst_AndLimited()
    {
        Log.Add(Allowed("1"));
        Log.Add(Allowed("2"));
        Log.Add(Allowed("3"));
        Assert.Equal(["3", "2"], Log.Recent(2).Select(e => e.Number));
    }

    [Fact]
    public void Add_KeepsOnlyTheNewestEntries()
    {
        for (int i = 0; i < CallLogStore.MaxEntries + 5; i++) Log.Add(Allowed(i.ToString()));

        var all = Log.Recent(int.MaxValue);
        Assert.Equal(CallLogStore.MaxEntries, all.Count);
        Assert.Equal((CallLogStore.MaxEntries + 4).ToString(), all[0].Number);
        Assert.Equal("5", all[^1].Number);
    }

    [Fact]
    public void BlockedSince_CountsRejectedCalls_FromThatTime()
    {
        var now = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        Log.Add(Blocked(), now.AddDays(-10));
        Log.Add(Blocked(), now.AddDays(-1));
        Log.Add(Blocked(), now.AddHours(-1));
        Log.Add(Allowed(), now.AddHours(-1));

        Assert.Equal(2, Log.BlockedSince(now.AddDays(-7)));
        Assert.Equal(3, Log.BlockedSince(now.AddDays(-30)));
        Assert.Equal(0, Log.BlockedSince(now));
    }

    [Fact]
    public void AddError_IsLoggedAsAllowed()
    {
        Log.AddError("database locked");
        var e = Assert.Single(Log.Recent());
        Assert.Equal(ScreeningAction.Allow, e.Action);
        Assert.Equal("allowed  ERROR database locked (call allowed)", e.Describe());
    }

    [Fact]
    public void LongReasons_AreTruncated()
    {
        Log.AddError(new string('x', 1000));
        Assert.Equal(CallLogStore.MaxReasonLength, Assert.Single(Log.Recent()).Reason.Length);
    }

    [Fact]
    public void Clear_RemovesEverything_AndRaisesChanged()
    {
        int changes = 0;
        Log.Changed += () => changes++;

        Log.Add(Blocked());
        Log.Clear();

        Assert.Empty(Log.Recent());
        Assert.Equal(2, changes);
    }
}
