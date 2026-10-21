using Android.Content;
using CallGuard.Data;

namespace CallGuard;

/// <summary>
/// The app's storage: patterns and the call log in a SQLite database (via EF Core). The app's private
/// SharedPreferences only remember which database schema version was last set up.
/// </summary>
public static class Store
{
    const string PrefsName = "callguard";
    const string DatabaseName = "callguard.db";
    static readonly object DbLock = new();
    static CallGuardDatabase? database;

    static Context Ctx => Android.App.Application.Context;

    /// <summary>The database, created or upgraded (only when the schema changed) on first use in this process.</summary>
    static CallGuardDatabase Database
    {
        get
        {
            lock (DbLock)
            {
                if (database == null)
                {
                    var db = new CallGuardDatabase(DatabasePath);
                    if (db.MigrateIfNeeded(Prefs.GetString("schemaVersion", null)))
                    {
                        var edit = Prefs.Edit()!.PutString("schemaVersion", CallGuardDatabase.SchemaVersion)!;
                        // Settings from earlier versions; patterns are now switched on and off one by one.
                        foreach (var key in new[] { "enabled", "blockHidden", "blockInternational", "blockFailedVerification", "silence" })
                            edit = edit.Remove(key)!;
                        edit.Commit();
                    }
                    database = db;
                }
                return database;
            }
        }
    }

    public static PatternStore Patterns => Database.Patterns;

    static string DatabasePath => Path.Combine(Ctx.FilesDir!.AbsolutePath, DatabaseName);

    /// <summary>
    /// The patterns for screening a call. When the database is already at the current schema this
    /// doesn't touch EF Core at all, which keeps call-triggered starts fast.
    /// </summary>
    public static PatternSet PatternsForScreening()
    {
        var path = DatabasePath;
        if (Prefs.GetString("schemaVersion", null) == CallGuardDatabase.SchemaVersion && File.Exists(path))
            return ScreeningPatternReader.Load(path);
        return Database.LoadPatternsForScreening(); // creates or upgrades the database first
    }
    public static CallLogStore CallLog => Database.CallLog;

    static ISharedPreferences Prefs => Ctx.GetSharedPreferences(PrefsName, FileCreationMode.Private)!;
}
