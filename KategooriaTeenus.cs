using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

internal static class KategooriaTeenus
{
    internal static void Kustuta(ToodedContext db, int id)
    {
        if (!AktiivneKasutaja.OnAdmin || AktiivneKasutaja.OnDemo)
            throw new InvalidOperationException("Kategooriat saab kustutada ainult administraator.");
        // Üks SQL-lause: kontroll ja kustutamine toimuvad koos ka vana CASCADE skeemi korral.
        int count = db.Kategooriatabel
            .Where(k => k.Id == id && !db.Toodetabel.Any(t => t.KategooriaId == k.Id))
            .ExecuteDelete();
        if (count == 0)
            throw new InvalidOperationException("Kategooriat ei leitud või selles on tooteid. Tõsta tooted enne teise kategooriasse.");
    }
}
