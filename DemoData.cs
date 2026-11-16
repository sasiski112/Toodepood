using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

internal static class DemoData
{
    internal static readonly Dictionary<string, string> Kirjeldused = new()
    {
        ["Värsked apelsinid"] = "Mahlased ja magusad apelsinid, mis sobivad nii värskelt söömiseks kui ka mahla pressimiseks. Rikkalik C-vitamiini allikas. Hoia jahedas ja kuivas kohas ning pese koor enne kasutamist.",
        ["Rohelised õunad"] = "Krõmpsuvad hapukasmagusad rohelised õunad. Sobivad suupisteks, salatisse ja küpsetamiseks. Säilivad külmikus mitu nädalat.",
        ["Avokaado"] = "Küps ja kreemja tekstuuriga avokaado. Sobib võileivale, salatisse ja guacamole'ks. Kõva vilja lase toatemperatuuril paar päeva küpseda.",
        ["Värsked tomatid"] = "Aromaatsed punased tomatid salatiks, võileivale ja kastmetesse. Hoia toatemperatuuril, et maitse säiliks.",
        ["Apelsinimahl"] = "Hoidmata apelsinimahl ilma lisatud suhkruta. Raputa enne kasutamist ja hoia avatuna külmkapis, tarbi kahe päeva jooksul.",
        ["Mineraalvesi"] = "Looduslik mineraalvesi, mis värskendab nii hommikul kui ka pärast trenni. Saadaval gaseeritult ja jahedana serveerituna on kõige parem.",
        ["Tume šokolaad"] = "Rikkaliku kakaomaitsega tume šokolaad. Sobib magustoiduks, küpsetamiseks või väikeseks nautimiseks kohvi kõrvale. Hoia jahedas.",
        ["Võiküpsised"] = "Murused võiküpsised päris võiga. Sobivad teele, kohvi kõrvale ja pühadelauale. Pärast avamist hoia õhukindlas anumas.",
    };

    private sealed record Profiil(double Baas, double[] Nadalapaev, double[] Tunnid, double Kasv, Dictionary<string, double> Pyhad);

    // Nädalapäevad: esmaspäev … pühapäev; tunnid: 8 … 21
    private static readonly double[] KoogiviliPaev = { 1.0, 0.9, 0.9, 1.0, 1.2, 1.5, 0.9 };
    private static readonly double[] JoogidPaev = { 0.8, 0.8, 0.9, 1.0, 1.4, 1.5, 1.2 };
    private static readonly double[] MaiustusedPaev = { 0.8, 0.8, 0.9, 1.0, 1.4, 1.5, 1.3 };
    private static readonly double[] KoogiviliTunnid = { 3, 4, 4, 3, 2, 2, 3, 5, 6, 5, 3, 2, 1, 1 };
    private static readonly double[] JoogidTunnid = { 1, 2, 2, 3, 3, 4, 5, 6, 6, 5, 4, 3, 2, 1 };
    private static readonly double[] MaiustusedTunnid = { 1, 1, 2, 2, 2, 3, 4, 5, 6, 6, 6, 5, 3, 2 };

    private static readonly Dictionary<string, Profiil> Profiilid = new()
    {
        ["Värsked apelsinid"] = new(2.0, KoogiviliPaev, KoogiviliTunnid, 1.0, new() { ["Jõulud"] = 2.5, ["Vana-aasta õhtu"] = 1.4 }),
        ["Rohelised õunad"] = new(2.5, KoogiviliPaev, KoogiviliTunnid, 0.85, new() { ["Mardipäev"] = 1.8, ["Jõulud"] = 1.3 }),
        ["Avokaado"] = new(1.2, KoogiviliPaev, KoogiviliTunnid, 1.6, new() { ["Vana-aasta õhtu"] = 1.6, ["Jaanipäev"] = 1.4 }),
        ["Värsked tomatid"] = new(1.8, KoogiviliPaev, KoogiviliTunnid, 1.0, new() { ["Jaanipäev"] = 1.9, ["Lihavõtted"] = 1.3 }),
        ["Apelsinimahl"] = new(1.6, JoogidPaev, JoogidTunnid, 1.0, new() { ["Jõulud"] = 1.5, ["Vana-aasta õhtu"] = 1.7, ["Lihavõtted"] = 1.3 }),
        ["Mineraalvesi"] = new(3.5, JoogidPaev, JoogidTunnid, 1.1, new() { ["Jaanipäev"] = 2.3, ["Vana-aasta õhtu"] = 1.5 }),
        ["Tume šokolaad"] = new(1.8, MaiustusedPaev, MaiustusedTunnid, 1.0,
            new() { ["Sõbrapäev"] = 2.8, ["Jõulud"] = 2.2, ["Lihavõtted"] = 2.0, ["Emadepäev"] = 1.8, ["Kadripäev"] = 1.6, ["Mardipäev"] = 1.3 }),
        ["Võiküpsised"] = new(1.5, MaiustusedPaev, MaiustusedTunnid, 1.0,
            new() { ["Jõulud"] = 2.4, ["Kadripäev"] = 1.7, ["Mardipäev"] = 1.4, ["Lihavõtted"] = 1.5, ["Emadepäev"] = 1.6 }),
    };

    internal static void Seed(ToodedContext db)
    {
        db.Toodetabel.AddRange(
            Uus("Värsked apelsinid", 2.90m, 24, 2), Uus("Rohelised õunad", 1.80m, 36, 2),
            Uus("Avokaado", 2.40m, 4, 2), Uus("Värsked tomatid", 3.20m, 18, 2),
            Uus("Apelsinimahl", 3.50m, 16, 1), Uus("Mineraalvesi", 1.20m, 48, 1),
            Uus("Tume šokolaad", 2.60m, 20, 3), Uus("Võiküpsised", 3.90m, 0, 3));
        db.SaveChanges();
    }

    private static Toode Uus(string nimi, decimal hind, int kogus, int kategooria) => new()
    { Toodenimetus = nimi, Hind = hind, Kogus = kogus, KategooriaId = kategooria, Kirjeldus = Kirjeldused[nimi] };

    // Olemasolevale andmebaasile lisatakse kirjeldused ainult demotoodetele, millel kirjeldust veel ei ole
    internal static void TaidaKirjeldused(ToodedContext db)
    {
        foreach (var p in db.Toodetabel.Where(t => t.Kirjeldus == null || t.Kirjeldus == "").ToList())
            if (Kirjeldused.TryGetValue(p.Toodenimetus, out var k)) p.Kirjeldus = k;
        db.SaveChanges();
    }

    // Kaks aastat demomüüki (märgitud Demo = true), et soovitused oleksid kohe näha.
    // Mustrid (nädalapäev, kellaaeg, pühad) on siin sisse kirjutatud; analüüs õpib need ajaloost ise uuesti kätte.
    internal static void LooAjalugu(ToodedContext db)
    {
        if (db.Muugid.Any()) return;
        var rnd = new Random(20261109);
        var tana = DateTime.Today; var alg = tana.AddDays(-730);
        var read = new List<Muuk>();
        foreach (var t in db.Toodetabel.AsNoTracking().ToList())
        {
            if (!Profiilid.TryGetValue(t.Toodenimetus, out var pr)) continue;
            for (var d = alg; d < tana; d = d.AddDays(1))
            {
                double kasv = 1 + (pr.Kasv - 1) * (d - alg).TotalDays / 730.0;
                double lambda = pr.Baas * pr.Nadalapaev[((int)d.DayOfWeek + 6) % 7] * kasv * (0.85 + rnd.NextDouble() * 0.3);
                var pyha = Pyhad.Aktiivne(d);
                if (pyha != null && pr.Pyhad.TryGetValue(pyha.Nimi, out var tegur)) lambda *= tegur;
                int uhikuid = Poisson(rnd, lambda);
                while (uhikuid > 0)
                {
                    int q = Math.Min(uhikuid, 1 + rnd.Next(3));
                    read.Add(new Muuk { ToodeId = t.Id, Kogus = q, Demo = true,
                        Aeg = d.AddHours(Tund(rnd, pr.Tunnid)).AddMinutes(rnd.Next(60)) });
                    uhikuid -= q;
                }
            }
        }
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        try { db.Muugid.AddRange(read); db.SaveChanges(); }
        finally { db.ChangeTracker.AutoDetectChangesEnabled = true; }
    }

    private static int Poisson(Random rnd, double lambda)
    {
        double piir = Math.Exp(-lambda), p = 1; int k = 0;
        do { k++; p *= rnd.NextDouble(); } while (p > piir);
        return k - 1;
    }

    private static int Tund(Random rnd, double[] kaalud)
    {
        double x = rnd.NextDouble() * kaalud.Sum();
        for (int i = 0; i < kaalud.Length; i++) { x -= kaalud[i]; if (x <= 0) return 8 + i; }
        return 8 + kaalud.Length - 1;
    }
}
