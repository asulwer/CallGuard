using CallGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CallGuard.Tests;

public sealed class PatternStoreTests : TempDatabase
{
    PatternStore Store => Db.Patterns;

    [Fact]
    public void NewDatabase_IsEmpty() => Assert.Empty(Store.List());

    [Fact]
    public void Add_StoresCleanedPattern()
    {
        Assert.Equal(AddPatternResult.Added, Store.Add("+1 (602) 555-****", label: "  spam  "));

        var saved = Assert.Single(Store.List());
        Assert.Equal("602555****", saved.Pattern);
        Assert.Equal(PatternAction.Block, saved.Action);
        Assert.Equal("spam", saved.Label);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_BlankLabel_IsStoredAsNull(string? label)
    {
        Store.Add("602*", label: label);
        Assert.Null(Assert.Single(Store.List()).Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("# comment")]
    public void Add_RejectsInvalidInput(string input)
    {
        Assert.Equal(AddPatternResult.Invalid, Store.Add(input));
        Assert.Empty(Store.List());
    }

    [Fact]
    public void Add_RejectsDuplicates_AfterCleaning()
    {
        Assert.Equal(AddPatternResult.Added, Store.Add("602*"));
        Assert.Equal(AddPatternResult.Duplicate, Store.Add("+1 602*"));
        Assert.Single(Store.List());
    }

    [Fact]
    public void Add_CountryAndAreaCodePatterns_AreDifferentPatterns()
    {
        Assert.Equal(AddPatternResult.Added, Store.Add("+44*", label: "UK"));
        Assert.Equal(AddPatternResult.Added, Store.Add("44*"));
        Assert.Equal(AddPatternResult.Duplicate, Store.Add("011 44*"));
        Assert.Equal(["+44*", "44*"], Store.List().Select(p => p.Pattern).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Add_SamePatternCannotBeBothBlockedAndAllowed()
    {
        Store.Add("602*");
        Assert.Equal(AddPatternResult.Duplicate, Store.Add("602*", PatternAction.Allow));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("1*")]
    [InlineData("??????????")]
    public void Add_PatternMatchingEveryNumber_NeedsConfirmation(string input)
    {
        Assert.Equal(AddPatternResult.MatchesEveryNumber, Store.Add(input));
        Assert.Empty(Store.List());

        Assert.Equal(AddPatternResult.Added, Store.Add(input, allowEveryNumber: true));
        Assert.Single(Store.List());
    }

    [Fact]
    public void Load_SplitsBlockAndAllow()
    {
        Store.Add("602*");
        Store.Add("6025550123", PatternAction.Allow);
        Store.Add("480*");

        var set = Store.Load();
        Assert.Equal(["480*", "602*"], set.Block);
        Assert.Equal(["6025550123"], set.Allow);
    }

    [Fact]
    public void List_PutsBlockedFirst_ThenSortsByPattern()
    {
        Store.Add("6025550123", PatternAction.Allow);
        Store.Add("602*");
        Store.Add("480*");
        Assert.Equal(["480*", "602*", "6025550123"], Store.List().Select(p => p.Pattern));
    }

    [Fact]
    public void NewPatterns_AreSwitchedOn()
    {
        Store.Add("602*");
        Assert.True(Assert.Single(Store.List()).IsEnabled);
    }

    [Fact]
    public void SwitchedOffPatterns_AreKeptButNotUsedForScreening()
    {
        Store.Add("602*");
        Store.Add("6025550123", PatternAction.Allow);
        Store.Add("480*");
        Store.SetEnabled(Store.List().Single(p => p.Pattern == "602*").Id, false);
        Store.SetEnabled(Store.List().Single(p => p.Pattern == "6025550123").Id, false);

        Assert.Equal(3, Store.List().Count);
        Assert.Equal(["480*"], Store.Load().Block);
        Assert.Empty(Store.Load().Allow);
        Assert.Equal(["480*"], ScreeningPatternReader.Load(DbPath).Block);
        Assert.Empty(ScreeningPatternReader.Load(DbPath).Allow);
    }

    [Fact]
    public void SetEnabled_SwitchesBackOn_AndRaisesChangedOnlyOnARealChange()
    {
        Store.Add("602*");
        var id = Store.List()[0].Id;
        int changes = 0;
        Store.Changed += () => changes++;

        Assert.True(Store.SetEnabled(id, false));
        Assert.True(Store.SetEnabled(id, false)); // already off: no change
        Assert.True(Store.SetEnabled(id, true));
        Assert.False(Store.SetEnabled(12345, false));

        Assert.Equal(2, changes);
        Assert.Equal(["602*"], Store.Load().Block);
    }

    [Fact]
    public void SetLabel_ChangesTrimsAndClearsTheNote()
    {
        Store.Add("602*", label: "old note");
        var id = Store.List()[0].Id;
        int changes = 0;
        Store.Changed += () => changes++;

        Assert.True(Store.SetLabel(id, "  Phoenix spam  "));
        Assert.Equal("Phoenix spam", Store.List()[0].Label);

        Assert.True(Store.SetLabel(id, "   "));
        Assert.Null(Store.List()[0].Label);

        Assert.False(Store.SetLabel(12345, "nobody"));
        Assert.Equal(2, changes);
        Assert.Equal("602*", Store.List()[0].Pattern); // the pattern itself is untouched
    }

    [Fact]
    public void Remove_DeletesOnlyThatPattern()
    {
        Store.Add("602*");
        Store.Add("480*");
        var id = Store.List().Single(p => p.Pattern == "602*").Id;

        Assert.True(Store.Remove(id));
        Assert.Equal(["480*"], Store.Load().Block);
        Assert.False(Store.Remove(id));
    }

    [Fact]
    public void Changed_IsRaised_OnlyWhenSomethingChanged()
    {
        int changes = 0;
        Store.Changed += () => changes++;

        Store.Add("602*");
        Store.Add("602*");     // duplicate
        Store.Add("abc");      // invalid
        Store.Remove(12345);   // no such id
        Assert.Equal(1, changes);

        Store.Remove(Store.List()[0].Id);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ExportThenImport_RoundTrips()
    {
        Store.Add("602*", label: "Phoenix spam");
        Store.Add("6025550123", PatternAction.Allow, "doctor");
        Store.Add("480*");
        Store.SetEnabled(Store.List().Single(p => p.Pattern == "480*").Id, false);
        var exported = Store.Export();

        foreach (var p in Store.List()) Store.Remove(p.Id);
        var result = Store.Import(exported);

        Assert.Equal(new ImportResult(Added: 3, Duplicates: 0, Invalid: 0), result);
        Assert.Collection(Store.List(),
            p => { Assert.Equal("480*", p.Pattern); Assert.False(p.IsEnabled); },
            p => { Assert.Equal("602*", p.Pattern); Assert.Equal(PatternAction.Block, p.Action); Assert.Equal("Phoenix spam", p.Label); Assert.True(p.IsEnabled); },
            p => { Assert.Equal("6025550123", p.Pattern); Assert.Equal(PatternAction.Allow, p.Action); Assert.Equal("doctor", p.Label); });
    }

    [Fact]
    public void Import_SkipsExistingAndCountsBadLines()
    {
        Store.Add("602*");
        var result = Store.Import("block 602*\nallow 480*\nnonsense\n602*\n");
        Assert.Equal(new ImportResult(Added: 1, Duplicates: 2, Invalid: 1), result);
        Assert.Equal(["480*"], Store.Load().Allow);
    }

    [Fact]
    public void Patterns_PersistAcrossDatabaseInstances()
    {
        Store.Add("602*");

        var reopened = new CallGuardDatabase(DbPath);
        reopened.Migrate(); // running migrations again on an up-to-date database is harmless
        Assert.Equal(["602*"], reopened.Patterns.Load().Block);
    }

    [Fact]
    public void StoredPatterns_DriveTheScreeningDecision()
    {
        Store.Add("602*");
        Store.Add("6025550123", PatternAction.Allow);

        ScreeningAction Screen(string n) =>
            ScreeningDecider.Decide(new(n), Store.Load, _ => ContactLookup.NotAContact).Action;

        Assert.Equal(ScreeningAction.Reject, Screen("+16025551234"));
        Assert.Equal(ScreeningAction.Allow, Screen("+16025550123"));
    }

    [Fact]
    public void LoadPatternsForScreening_MatchesEF()
    {
        Store.Add("602*");
        Store.Add("6025550123", PatternAction.Allow);
        Store.Add("480*");

        var fast = ScreeningPatternReader.Load(DbPath);
        var ef = Store.Load();
        Assert.Equal(ef.Block, fast.Block);
        Assert.Equal(ef.Allow, fast.Allow);
    }

    [Fact]
    public void LoadPatternsForScreening_EmptyDatabase() =>
        Assert.Empty(ScreeningPatternReader.Load(DbPath).Block);

    [Fact]
    public void UpgradingFromTheFirstVersion_KeepsExistingPatternsSwitchedOn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"callguard-test-{Guid.NewGuid():N}.db");
        try
        {
            using (var db = new CallGuardDbContext(CallGuardDbContext.OptionsFor(path)))
            {
                db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().Migrate("20261020211615_InitialCreate");
                db.Database.ExecuteSqlRaw(
                    "INSERT INTO Patterns (Pattern, Action, CreatedUtc) VALUES ('602*', 0, '2026-10-20 12:00:00')");
            }

            var upgraded = new CallGuardDatabase(path);
            Assert.True(upgraded.MigrateIfNeeded("20261020211615_InitialCreate"));
            Assert.Equal(["602*"], upgraded.Patterns.Load().Block);
            Assert.Equal(["602*"], ScreeningPatternReader.Load(path).Block);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public void SchemaVersion_IsTheLatestMigration()
    {
        // Fails if a migration was added without updating CallGuardDatabase.SchemaVersion.
        using var db = new CallGuardDbContext(CallGuardDbContext.OptionsFor(DbPath));
        Assert.Equal(CallGuardDatabase.SchemaVersion, db.Database.GetMigrations().Last());
    }

    [Fact]
    public void MigrateIfNeeded_SkipsWhenAlreadyAtThisVersion()
    {
        Assert.False(new CallGuardDatabase(DbPath).MigrateIfNeeded(CallGuardDatabase.SchemaVersion));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("20200101000000_SomethingOlder")]
    public void MigrateIfNeeded_MigratesWhenTheVersionDiffers(string? recorded)
    {
        Assert.True(new CallGuardDatabase(DbPath).MigrateIfNeeded(recorded));
    }

    [Fact]
    public void MigrateIfNeeded_CreatesTheDatabase_WhenTheFileIsGone_EvenIfTheVersionMatches()
    {
        var path = Path.Combine(Path.GetTempPath(), $"callguard-test-{Guid.NewGuid():N}.db");
        try
        {
            var db = new CallGuardDatabase(path);
            Assert.True(db.MigrateIfNeeded(CallGuardDatabase.SchemaVersion));
            Assert.Equal(AddPatternResult.Added, db.Patterns.Add("602*"));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public void Migrations_MatchTheModel()
    {
        // Fails if the model was changed without running "dotnet ef migrations add".
        using var db = new CallGuardDbContext(CallGuardDbContext.OptionsFor(DbPath));
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
