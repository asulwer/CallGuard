using Microsoft.EntityFrameworkCore;

namespace CallGuard.Data;

/// <summary>The app's local SQLite database: saved patterns and the recent-calls log.</summary>
public sealed class CallGuardDatabase
{
    /// <summary>
    /// The latest migration. Update it whenever you add one; a unit test checks it.
    /// </summary>
    public const string SchemaVersion = "20261021005306_AddPatternEnabled";

    readonly DbContextOptions<CallGuardDbContext> options;
    readonly string? databasePath;

    public CallGuardDatabase(string databasePath) : this(CallGuardDbContext.OptionsFor(databasePath))
    {
        this.databasePath = databasePath;
    }

    public CallGuardDatabase(DbContextOptions<CallGuardDbContext> options)
    {
        this.options = options;
        Patterns = new PatternStore(options);
        CallLog = new CallLogStore(options);
    }

    public PatternStore Patterns { get; }
    public CallLogStore CallLog { get; }

    /// <summary>Creates the database or upgrades it to the latest schema.</summary>
    public void Migrate()
    {
        using var db = new CallGuardDbContext(options);
        db.Database.Migrate();
    }

    /// <summary>The patterns for screening a call; see <see cref="ScreeningPatternReader"/>.</summary>
    public PatternSet LoadPatternsForScreening() =>
        databasePath == null ? Patterns.Load() : ScreeningPatternReader.Load(databasePath);

    /// <summary>
    /// Migrates only when needed. Checking with <see cref="Migrate"/> costs a second or more on a phone,
    /// too slow when the app was just started to screen a call, so the caller remembers which
    /// <see cref="SchemaVersion"/> it last migrated to.
    /// </summary>
    /// <param name="migratedVersion">The version recorded after the last migration, or null if none.</param>
    /// <returns>True if it migrated; the caller should then record <see cref="SchemaVersion"/>.</returns>
    public bool MigrateIfNeeded(string? migratedVersion)
    {
        bool fileMissing = databasePath != null && !File.Exists(databasePath);
        if (migratedVersion == SchemaVersion && !fileMissing) return false;
        Migrate();
        return true;
    }
}
