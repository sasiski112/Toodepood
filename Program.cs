using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Kaustad.Loo();
            L.Load();
            using var db = new ToodedContext();
            var fresh = false; var upgraded = false;
            if (db.Database.IsSqlite())
            {
                fresh = db.Database.EnsureCreated();
                if (!fresh) upgraded = Andmebaas.UuendaSqlite(db);
            }
            else db.Database.Migrate();
            LooAlgkasutajad(db);
            if (fresh) DemoData.Seed(db);
            if (upgraded) DemoData.TaidaKirjeldused(db);
            if ((fresh || upgraded) && db.Database.IsSqlite()) DemoData.LooAjalugu(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Rakenduse käivitamine ebaõnnestus.\n" + ex.Message, "Tooded", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Application.Run(new LoginForm());
    }

    private static void LooAlgkasutajad(ToodedContext db)
    {
        if (db.Kasutajad.Any()) return;

        db.Kasutajad.AddRange(
            new Kasutaja { Nimi = "Admin", Email = "admin@admin.ee",
                           ParoolHash = PasswordHelper.Hash("admin123"), Roll = "admin" },
            new Kasutaja { Nimi = "Kasutaja", Email = "kasutaja@pood.ee",
                           ParoolHash = PasswordHelper.Hash("kasutaja123"), Roll = "kasutaja" });
        db.SaveChanges();
    }
}
