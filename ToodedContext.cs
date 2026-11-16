using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

public class ToodedContext : DbContext
{
    public ToodedContext() { }
    public ToodedContext(DbContextOptions<ToodedContext> options) : base(options) { }

    public DbSet<Toode> Toodetabel => Set<Toode>();
    public DbSet<Kategooria> Kategooriatabel => Set<Kategooria>();
    public DbSet<Kasutaja> Kasutajad => Set<Kasutaja>();
    public DbSet<Muuk> Muugid => Set<Muuk>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        var connection = Environment.GetEnvironmentVariable("TOODED_SQLSERVER");
        if (!string.IsNullOrWhiteSpace(connection)) optionsBuilder.UseSqlServer(connection);
        else optionsBuilder.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        { DataSource = Path.Combine(Kaustad.Juur, "tooded.db") }.ToString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kategooria>().ToTable("Kategooriatabel");
        modelBuilder.Entity<Toode>().ToTable("Toodetabel");
        modelBuilder.Entity<Muuk>(e =>
        {
            e.ToTable("Muugid");
            e.HasOne(m => m.Toode).WithMany().HasForeignKey(m => m.ToodeId).OnDelete(DeleteBehavior.Cascade);
        });

        // Algandmed: ilma kategooriateta ei saa toodet lisada
        modelBuilder.Entity<Kategooria>().HasData(
            new Kategooria { Id = 1, Kategooria_nimetus = "Joogid" },
            new Kategooria { Id = 2, Kategooria_nimetus = "Puu- ja köögiviljad" },
            new Kategooria { Id = 3, Kategooria_nimetus = "Maiustused" });
    }
}
