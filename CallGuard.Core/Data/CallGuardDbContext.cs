using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CallGuard.Data;

public class CallGuardDbContext(DbContextOptions<CallGuardDbContext> options) : DbContext(options)
{
    public DbSet<NumberPattern> Patterns => Set<NumberPattern>();
    public DbSet<CallLogEntry> CallLog => Set<CallLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NumberPattern>(e =>
        {
            e.ToTable("Patterns");
            e.Property(p => p.Pattern).HasMaxLength(32);
            e.HasIndex(p => p.Pattern).IsUnique();
            e.Property(p => p.Label).HasMaxLength(100);
        });

        modelBuilder.Entity<CallLogEntry>(e =>
        {
            e.ToTable("CallLog");
            e.Property(c => c.Number).HasMaxLength(32);
            e.Property(c => c.Reason).HasMaxLength(CallLogStore.MaxReasonLength);
            e.HasIndex(c => c.TimeUtc);
        });
    }

    public static DbContextOptions<CallGuardDbContext> OptionsFor(string databasePath) =>
        new DbContextOptionsBuilder<CallGuardDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString())
            .Options;
}

/// <summary>Lets the dotnet-ef tool build the model to generate migrations. Not used at runtime.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CallGuardDbContext>
{
    public CallGuardDbContext CreateDbContext(string[] args) => new(CallGuardDbContext.OptionsFor("design.db"));
}
