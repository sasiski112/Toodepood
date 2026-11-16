using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

internal static class Ostuteenus
{
    // Tingimuslik UPDATE ja müügiread kuuluvad samasse tehingusse.
    // Nii ei saa kaks rakendust sama viimast toodet korraga müüa.
    internal static void Kinnita(ToodedContext db, IReadOnlyList<OstukorviRida> read)
    {
        if (AktiivneKasutaja.OnAdmin || AktiivneKasutaja.OnDemo || read.Count == 0 ||
            read.Any(r => r.Kogus <= 0 || r.Toode.Hind < 0) ||
            read.Select(r => r.Toode.Id).Distinct().Count() != read.Count)
            throw new InvalidOperationException("Kontrolli ostukorvi.");

        using var transaction = db.Database.BeginTransaction();
        var aeg = DateTime.Now;
        foreach (var row in read.OrderBy(r => r.Toode.Id))
        {
            int id = row.Toode.Id, kogus = row.Kogus;
            decimal hind = row.Toode.Hind;
            int updated = db.Toodetabel
                .Where(p => p.Id == id && p.Kogus >= kogus && p.Hind == hind)
                .ExecuteUpdate(setters => setters.SetProperty(p => p.Kogus, p => p.Kogus - kogus));
            if (updated != 1)
                throw new InvalidOperationException($"Laoseis või hind on muutunud: {row.Toode.Toodenimetus}. Kontrolli ostukorvi poes.");
        }

        db.Muugid.AddRange(read.Select(row => new Muuk
        { ToodeId = row.Toode.Id, Kogus = row.Kogus, Aeg = aeg }));
        db.SaveChanges();
        transaction.Commit();
    }
}
