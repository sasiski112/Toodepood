using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tooded_EFCore;

var folder = Path.Combine(Path.GetTempPath(), "Tooded-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
var options = new DbContextOptionsBuilder<ToodedContext>().UseSqlite(
    new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "checks.db") }.ToString()).Options;
int checks = 0;
void Check(bool ok, string name)
{
    if (!ok) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); checks++;
}
void Rejected(Action action, string name)
{
    try { action(); } catch (InvalidOperationException) { Check(true, name); return; }
    throw new Exception("FAIL (accepted): " + name);
}
Toode Product(int id, int stock = 5, decimal price = 2m) => new()
{ Id = id, Toodenimetus = "Test " + id, Kogus = stock, Hind = price, KategooriaId = 1 };
OstukorviRida Row(int id, int quantity, decimal price = 2m) => new() { Toode = Product(id, price: price), Kogus = quantity };
try
{
    using (var db = new ToodedContext(options))
    {
        db.Database.EnsureCreated();
        Check(db.Kategooriatabel.Count() == 3, "database and categories");
        db.Toodetabel.AddRange(Product(1), Product(2)); db.SaveChanges();
        Check(!Andmebaas.UuendaSqlite(db), "current schema upgrade is idempotent");
    }
    using (var db = new ToodedContext(options)) Ostuteenus.Kinnita(db, new[] { Row(1, 2) });
    using (var db = new ToodedContext(options))
    {
        Check(db.Toodetabel.Find(1)!.Kogus == 3 && db.Muugid.Single().Kogus == 2, "purchase updates stock and sale");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, 1), Row(2, 6) }), "insufficient stock rejects whole purchase");
    }
    using (var db = new ToodedContext(options))
    {
        Check(db.Toodetabel.Find(1)!.Kogus == 3 && db.Muugid.Count() == 1, "rollback restores earlier stock updates");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, 1, 99m) }), "changed price rejected");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, -2) }), "negative quantity rejected");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, 0) }), "zero quantity rejected");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(99, 1) }), "deleted product rejected");
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, 1), Row(1, 1) }), "duplicate rows rejected");
        Rejected(() => Ostuteenus.Kinnita(db, Array.Empty<OstukorviRida>()), "empty purchase rejected");
        AktiivneKasutaja.Roll = "admin";
        Rejected(() => Ostuteenus.Kinnita(db, new[] { Row(1, 1) }), "administrator purchase rejected");
        AktiivneKasutaja.Roll = "kasutaja";
    }
    // A second context retains a stale entity while the first buyer takes the remaining stock.
    using (var stale = new ToodedContext(options))
    {
        var old = stale.Toodetabel.Find(1)!;
        using (var other = new ToodedContext(options)) Ostuteenus.Kinnita(other, new[] { Row(1, 3) });
        Rejected(() => Ostuteenus.Kinnita(stale, new[] { new OstukorviRida { Toode = old, Kogus = 1 } }), "stale context cannot oversell");
    }
    using (var db = new ToodedContext(options))
        Check(db.Toodetabel.Find(1)!.Kogus == 0 && db.Muugid.Count() == 2, "rejected purchases create no sales");
    Ostukorv.Tyhjenda();
    Check(!Ostukorv.Lisa(Product(2), -1), "cart rejects negative quantity");
    Check(Ostukorv.Lisa(Product(2), 2) && !Ostukorv.Lisa(Product(2), 4), "cart enforces total stock");
    Check(Ostukorv.Lisa(Product(2, price: 3m), 1) && Ostukorv.Kokku == 9m, "cart refreshes product price");
    Ostukorv.Tyhjenda();
    using (var db = new ToodedContext(options))
    {
        db.Database.ExecuteSqlRaw("DROP TABLE Muugid");
        db.Database.ExecuteSqlRaw("ALTER TABLE Toodetabel DROP COLUMN Kirjeldus");
        Check(Andmebaas.UuendaSqlite(db), "old SQLite schema upgraded");
        Check(db.Toodetabel.Count() == 2 && !Andmebaas.UuendaSqlite(db), "upgrade preserves products and is repeatable");
        DemoData.Seed(db); DemoData.LooAjalugu(db);
        int count = db.Muugid.Count(); DemoData.LooAjalugu(db);
        Check(count > 1000 && db.Muugid.Count() == count, "demo history seeds once");
        var results = Soovitused.Arvuta(db.Toodetabel.ToList(), db.Muugid.Select(m => new MuukPunkt(m.ToodeId, m.Kogus, m.Aeg)).ToList(), DateTime.Now, 14);
        Check(results.Count == 10 && results.Any(r => r.OnAndmeid) && results.All(r => double.IsFinite(r.Noudlus)), "recommendations calculate from seeded history");
    }
    AktiivneKasutaja.Nimi = "Admin"; AktiivneKasutaja.Roll = "admin";
    var originalRow = Row(2, 1);
    Ostukorv.Ridad.Add(originalRow);
    using (var session = new DemoSession())
    {
        Check(AktiivneKasutaja.OnDemo && !AktiivneKasutaja.OnAdmin && AktiivneKasutaja.Nimi == "Demo klient", "guest demo has customer role");
        Check(Ostukorv.Ridad.Count == 0 && Ostukorv.Lisa(Product(2), 1), "guest demo has independent working cart");
        using var db = new ToodedContext(options);
        int users = db.Kasutajad.Count(), sales = db.Muugid.Count(), stock = db.Toodetabel.Find(2)!.Kogus;
        Rejected(() => Ostuteenus.Kinnita(db, Ostukorv.Ridad), "guest demo cannot write real purchases");
        Check(db.Kasutajad.Count() == users && db.Muugid.Count() == sales && db.Toodetabel.Find(2)!.Kogus == stock, "demo creates no account or sale and preserves stock");
        Rejected(() => { using var nested = new DemoSession(); }, "nested demo session rejected");
    }
    Check(AktiivneKasutaja.OnAdmin && !AktiivneKasutaja.OnDemo && AktiivneKasutaja.Nimi == "Admin" && Ostukorv.Ridad.Single() == originalRow, "demo close restores administrator and original cart");
    try { using var session = new DemoSession(); throw new Exception("demo failure"); } catch (Exception ex) when (ex.Message == "demo failure") { }
    Check(AktiivneKasutaja.OnAdmin && !AktiivneKasutaja.OnDemo && Ostukorv.Ridad.Single() == originalRow, "demo failure restores administrator");
    using (var db = new ToodedContext(options))
    {
        var category = new Kategooria { Kategooria_nimetus = "Temporary" };
        db.Kategooriatabel.Add(category); db.SaveChanges();
        int products = db.Toodetabel.Count(), sales = db.Muugid.Count();
        Rejected(() => KategooriaTeenus.Kustuta(db, 1), "occupied category deletion rejected");
        Check(db.Toodetabel.Count() == products && db.Muugid.Count() == sales, "category deletion preserves products and sales");
        AktiivneKasutaja.Roll = "kasutaja";
        Rejected(() => KategooriaTeenus.Kustuta(db, category.Id), "customer cannot delete category");
        AktiivneKasutaja.Roll = "admin";
        KategooriaTeenus.Kustuta(db, category.Id);
        Check(!db.Kategooriatabel.AsNoTracking().Any(k => k.Id == category.Id), "empty category deleted");
        Rejected(() => KategooriaTeenus.Kustuta(db, category.Id), "missing category handled");
    }
    var receiptRow = Row(2, 3);
    var savedReceipt = new Receipt(new[] { receiptRow }, false);
    receiptRow.Kogus = 99; receiptRow.Toode.Hind = 88m;
    Check(savedReceipt.Total == 6m && savedReceipt.Lines[0].Quantity == 3, "receipt snapshot survives cart and price changes");
    Check(savedReceipt.Number != new Receipt(new[] { Row(2, 1) }, false).Number, "receipt numbers are distinct");
    Receipt.Last = savedReceipt;
    using (var demo = new DemoSession())
    {
        Check(Receipt.Last == null, "demo cannot see administrator receipt");
        Receipt.Last = new Receipt(new[] { Row(2, 1) }, true);
        Check(Receipt.Last.Demo, "demo receipt is marked");
    }
    Check(ReferenceEquals(Receipt.Last, savedReceipt), "demo close restores previous receipt");
    Console.WriteLine($"{checks} checks passed.");
}
finally
{
    Ostukorv.Tyhjenda(); SqliteConnection.ClearAllPools(); Directory.Delete(folder, true);
}

// Only the image-dependent folder helper is substituted; database/cart logic is linked unchanged.
namespace Tooded_EFCore { internal static class Kaustad { internal static string Juur => Path.GetTempPath(); } }
