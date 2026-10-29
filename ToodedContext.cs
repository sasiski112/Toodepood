using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

public class ToodedContext : DbContext
{
    public DbSet<Toode> Toodetabel => Set<Toode>();
    public DbSet<Kategooria> Kategooriatabel => Set<Kategooria>();
    public DbSet<Kasutaja> Kasutajad => Set<Kasutaja>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connection = Environment.GetEnvironmentVariable("TOODED_SQLSERVER");
        if (!string.IsNullOrWhiteSpace(connection)) optionsBuilder.UseSqlServer(connection);
        else optionsBuilder.UseSqlite($"Data Source={Path.Combine(Kaustad.Juur, "tooded.db")}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kategooria>().ToTable("Kategooriatabel");
        modelBuilder.Entity<Toode>().ToTable("Toodetabel");

        // Algandmed: ilma kategooriateta ei saa toodet lisada
        modelBuilder.Entity<Kategooria>().HasData(
            new Kategooria { Id = 1, Kategooria_nimetus = "Joogid" },
            new Kategooria { Id = 2, Kategooria_nimetus = "Puu- ja köögiviljad" },
            new Kategooria { Id = 3, Kategooria_nimetus = "Maiustused" });
    }
}
