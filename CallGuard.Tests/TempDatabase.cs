using CallGuard.Data;
using Microsoft.Data.Sqlite;

namespace CallGuard.Tests;

/// <summary>A real SQLite database file, created through the app's migrations and deleted afterwards.</summary>
public abstract class TempDatabase : IDisposable
{
    protected readonly string DbPath = Path.Combine(Path.GetTempPath(), $"callguard-test-{Guid.NewGuid():N}.db");
    protected readonly CallGuardDatabase Db;

    protected TempDatabase()
    {
        Db = new CallGuardDatabase(DbPath);
        Db.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools(); // release the file so it can be deleted
        File.Delete(DbPath);
        GC.SuppressFinalize(this);
    }
}
