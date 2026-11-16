namespace Tooded_EFCore;

// Aken = mitu päeva enne püha (püha ise kaasa arvatud) on ostuaktiivsus tavaliselt kõrgem
internal sealed record Pyha(string Nimi, DateTime Kuupaev, int Aken);

internal static class Pyhad
{
    private static readonly Dictionary<int, List<Pyha>> cache = new();

    internal static IReadOnlyList<Pyha> Aastas(int aasta)
    {
        lock (cache)
        {
            if (cache.TryGetValue(aasta, out var olemas)) return olemas;
            var list = new List<Pyha>
            {
                new("Uus aasta", new DateTime(aasta, 1, 1), 0),
                new("Sõbrapäev", new DateTime(aasta, 2, 14), 3),
                new("Iseseisvuspäev", new DateTime(aasta, 2, 24), 2),
                new("Naistepäev", new DateTime(aasta, 3, 8), 2),
                new("Lihavõtted", Lihavotted(aasta), 4),
                new("Kevadpüha", new DateTime(aasta, 5, 1), 1),
                new("Emadepäev", NdPuhapaev(aasta, 5, 2), 3),
                new("Jaanipäev", new DateTime(aasta, 6, 24), 4),
                new("Taasiseseisvumispäev", new DateTime(aasta, 8, 20), 1),
                new("Isadepäev", NdPuhapaev(aasta, 11, 2), 2),
                new("Mardipäev", new DateTime(aasta, 11, 10), 2),
                new("Kadripäev", new DateTime(aasta, 11, 25), 2),
                new("Jõulud", new DateTime(aasta, 12, 24), 6),
                new("Vana-aasta õhtu", new DateTime(aasta, 12, 31), 3),
            };
            cache[aasta] = list;
            return list;
        }
    }

    // Püha, mille ostuaknasse see päev kuulub (lähim püha võidab); muidu null
    internal static Pyha? Aktiivne(DateTime paev)
    {
        paev = paev.Date;
        Pyha? parim = null; int parimKaugus = int.MaxValue;
        for (int aasta = paev.Year; aasta <= paev.Year + 1; aasta++)
            foreach (var p in Aastas(aasta))
            {
                int kaugus = (p.Kuupaev - paev).Days;
                if (kaugus >= 0 && kaugus <= p.Aken && kaugus < parimKaugus) { parim = p; parimKaugus = kaugus; }
            }
        return parim;
    }

    // Pühad, mille kuupäev jääb vahemikku [alates, alates + paeva]
    internal static List<Pyha> Lahedased(DateTime alates, int paeva)
    {
        var algus = alates.Date; var lopp = algus.AddDays(paeva);
        return Aastas(algus.Year).Concat(Aastas(algus.Year + 1))
            .Where(p => p.Kuupaev >= algus && p.Kuupaev <= lopp).OrderBy(p => p.Kuupaev).ToList();
    }

    private static DateTime NdPuhapaev(int aasta, int kuu, int mitmes)
    {
        var d = new DateTime(aasta, kuu, 1);
        while (d.DayOfWeek != DayOfWeek.Sunday) d = d.AddDays(1);
        return d.AddDays(7 * (mitmes - 1));
    }

    // Gregoriuse kalendri lihavõtted (Meeus/Jones/Butcher)
    private static DateTime Lihavotted(int y)
    {
        int a = y % 19, b = y / 100, c = y % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int kuu = (h + l - 7 * m + 114) / 31, paev = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(y, kuu, paev);
    }
}
