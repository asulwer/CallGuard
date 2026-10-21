using Microsoft.EntityFrameworkCore;

namespace CallGuard.Data;

public enum AddPatternResult
{
    Added,
    Duplicate,
    Invalid,
    /// <summary>The pattern has no digits, so it would match every number. Pass allowEveryNumber to add it anyway.</summary>
    MatchesEveryNumber,
}

public sealed record ImportResult(int Added, int Duplicates, int Invalid);

/// <summary>Reads and writes the saved block / always-allow patterns.</summary>
public sealed class PatternStore(DbContextOptions<CallGuardDbContext> options)
{
    /// <summary>Raised after patterns are added or removed.</summary>
    public event Action? Changed;

    /// <summary>All patterns, blocked first, each group sorted by pattern.</summary>
    public List<NumberPattern> List()
    {
        using var db = Open();
        return db.Patterns.AsNoTracking().OrderBy(p => p.Action).ThenBy(p => p.Pattern).ToList();
    }

    /// <summary>The patterns that are switched on, for screening.</summary>
    public PatternSet Load()
    {
        using var db = Open();
        var all = db.Patterns.AsNoTracking().Where(p => p.IsEnabled).OrderBy(p => p.Pattern)
            .Select(p => new { p.Pattern, p.Action }).ToList();
        return new PatternSet(
            all.Where(p => p.Action == PatternAction.Block).Select(p => p.Pattern).ToList(),
            all.Where(p => p.Action == PatternAction.Allow).Select(p => p.Pattern).ToList());
    }

    /// <summary>Cleans the user's input (see <see cref="RuleEngine.CleanPattern"/>) and saves it.</summary>
    public AddPatternResult Add(string? input, PatternAction action = PatternAction.Block,
                                string? label = null, bool allowEveryNumber = false)
    {
        var pattern = RuleEngine.CleanPattern(input);
        if (pattern == null) return AddPatternResult.Invalid;
        if (!allowEveryNumber && RuleEngine.MatchesEveryNumber(pattern)) return AddPatternResult.MatchesEveryNumber;

        using (var db = Open())
        {
            if (db.Patterns.Any(p => p.Pattern == pattern)) return AddPatternResult.Duplicate;
            db.Patterns.Add(New(pattern, action, label));
            db.SaveChanges();
        }
        Changed?.Invoke();
        return AddPatternResult.Added;
    }

    /// <summary>Switches a pattern on or off. Returns false if there was no pattern with that id.</summary>
    public bool SetEnabled(int id, bool enabled)
    {
        int changed;
        using (var db = Open())
        {
            changed = db.Patterns.Where(p => p.Id == id && p.IsEnabled != enabled)
                .ExecuteUpdate(u => u.SetProperty(p => p.IsEnabled, enabled));
        }
        if (changed > 0) Changed?.Invoke();
        if (changed > 0) return true;
        using var check = Open();
        return check.Patterns.Any(p => p.Id == id);
    }

    /// <summary>Returns false if there was no pattern with that id.</summary>
    public bool Remove(int id)
    {
        int removed;
        using (var db = Open())
        {
            removed = db.Patterns.Where(p => p.Id == id).ExecuteDelete();
        }
        if (removed > 0) Changed?.Invoke();
        return removed > 0;
    }

    /// <summary>All patterns in the text format described in <see cref="PatternFile"/>.</summary>
    public string Export() => PatternFile.Write(List());

    /// <summary>Adds the patterns in a <see cref="PatternFile"/>; ones already saved are skipped.</summary>
    public ImportResult Import(string text)
    {
        int added = 0, duplicates = 0, invalid = 0;
        using (var db = Open())
        {
            var existing = db.Patterns.Select(p => p.Pattern).ToHashSet();
            foreach (var line in PatternFile.Parse(text))
            {
                if (line.Pattern == null) invalid++;
                else if (!existing.Add(line.Pattern)) duplicates++;
                else
                {
                    var pattern = New(line.Pattern, line.Action, line.Label);
                    pattern.IsEnabled = line.Enabled;
                    db.Patterns.Add(pattern);
                    added++;
                }
            }
            db.SaveChanges();
        }
        if (added > 0) Changed?.Invoke();
        return new ImportResult(added, duplicates, invalid);
    }

    static NumberPattern New(string pattern, PatternAction action, string? label) => new()
    {
        Pattern = pattern,
        Action = action,
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
        CreatedUtc = DateTime.UtcNow,
    };

    CallGuardDbContext Open() => new(options);
}
