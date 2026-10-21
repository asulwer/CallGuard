using Microsoft.Data.Sqlite;

namespace CallGuard.Data;

/// <summary>
/// Reads the saved patterns with one plain SQL query, without EF Core. EF's first query in a process
/// costs over a second on a phone (it builds its model), too slow when the app was just started to
/// screen a call. EF still owns the schema; a unit test checks this agrees with <see cref="PatternStore.Load"/>.
/// </summary>
public static class ScreeningPatternReader
{
    public static PatternSet Load(string databasePath)
    {
        var block = new List<string>();
        var allow = new List<string>();
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Pattern, Action FROM Patterns WHERE IsEnabled = 1 ORDER BY Pattern";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var list = (PatternAction)reader.GetInt32(1) == PatternAction.Allow ? allow : block;
            list.Add(reader.GetString(0));
        }
        return new PatternSet(block, allow);
    }
}
