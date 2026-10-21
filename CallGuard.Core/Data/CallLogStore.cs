using Microsoft.EntityFrameworkCore;

namespace CallGuard.Data;

/// <summary>The recent-calls log. Keeps the newest <see cref="MaxEntries"/> entries.</summary>
public sealed class CallLogStore(DbContextOptions<CallGuardDbContext> options)
{
    public const int MaxEntries = 500;
    public const int MaxReasonLength = 200;

    /// <summary>Raised after entries are added or cleared.</summary>
    public event Action? Changed;

    public void Add(ScreeningDecision decision, DateTime? timeUtc = null)
    {
        using (var db = Open())
        {
            db.CallLog.Add(new CallLogEntry
            {
                TimeUtc = timeUtc ?? DateTime.UtcNow,
                Number = decision.Number,
                Action = decision.Action,
                Reason = decision.Reason.Length > MaxReasonLength ? decision.Reason[..MaxReasonLength] : decision.Reason,
            });
            db.SaveChanges();

            var oldestKept = db.CallLog.OrderByDescending(e => e.Id).Skip(MaxEntries - 1).Select(e => (int?)e.Id).FirstOrDefault();
            if (oldestKept != null) db.CallLog.Where(e => e.Id < oldestKept).ExecuteDelete();
        }
        Changed?.Invoke();
    }

    /// <summary>Records that screening failed (the call was let through).</summary>
    public void AddError(string message, string number = "") =>
        Add(new ScreeningDecision(ScreeningAction.Allow, number, $"ERROR {message} (call allowed)"));

    /// <summary>Newest first.</summary>
    public List<CallLogEntry> Recent(int count = 100)
    {
        using var db = Open();
        return db.CallLog.AsNoTracking().OrderByDescending(e => e.Id).Take(count).ToList();
    }

    /// <summary>How many calls were blocked at or after the given time.</summary>
    public int BlockedSince(DateTime utc)
    {
        using var db = Open();
        return db.CallLog.Count(e => e.Action != ScreeningAction.Allow && e.TimeUtc >= utc);
    }

    public void Clear()
    {
        using (var db = Open())
        {
            db.CallLog.ExecuteDelete();
        }
        Changed?.Invoke();
    }

    CallGuardDbContext Open() => new(options);
}
