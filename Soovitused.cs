namespace Tooded_EFCore;

internal sealed record MuukPunkt(int ToodeId, int Kogus, DateTime Aeg);

internal enum Tahtsus { Kriitiline, Telli, Piisav, AndmeidVahe }

internal sealed class Soovitus
{
    internal Toode Toode { get; init; } = null!;
    internal Tahtsus Aste { get; set; }
    internal bool OnAndmeid { get; set; }
    internal double? Katab { get; set; }       // mitu päeva laoseis kestab (null = kestab kogu horisondi)
    internal double Noudlus { get; set; }      // oodatav müük horisondi jooksul
    internal double TanaVeel { get; set; }     // oodatav müük tänase päeva ülejäänud tundidel
    internal int? Telli { get; set; }
    internal string Pohjendus { get; set; } = "";
    internal string Trend { get; set; } = "";
}

// Prognoos müügiajaloost: baastase (värskemad päevad kaaluvad rohkem), nädalapäeva tegurid,
// pühade tegurid ja tunnijaotus. Ei kasuta väliseid andmeid ega eeldusi.
internal static class Soovitused
{
    private const int MinPaevi = 28;
    private const int MinMuuke = 10;
    private const double Varu = 1.15;   // 15% turvavaru
    private static readonly string[] PaevaNimed =
        { "pühapäev", "esmaspäev", "teisipäev", "kolmapäev", "neljapäev", "reede", "laupäev" };

    internal static List<Soovitus> Arvuta(IReadOnlyList<Toode> tooded, IEnumerable<MuukPunkt> muugid, DateTime nuud, int horisont)
    {
        var grupid = muugid.GroupBy(m => m.ToodeId).ToDictionary(g => g.Key, g => g.ToList());
        return tooded
            .Select(t => Hinda(t, grupid.TryGetValue(t.Id, out var l) ? l : new List<MuukPunkt>(), nuud, horisont))
            .OrderBy(s => s.Aste).ThenBy(s => s.Katab ?? double.MaxValue).ThenBy(s => s.Toode.Toodenimetus)
            .ToList();
    }

    private static Soovitus Hinda(Toode toode, List<MuukPunkt> koik, DateTime nuud, int horisont)
    {
        var tana = nuud.Date;
        var s = new Soovitus { Toode = toode };
        var ajalugu = koik.Where(m => m.Aeg.Date < tana).ToList();
        var alg = ajalugu.Count > 0 ? ajalugu.Min(m => m.Aeg.Date) : tana;
        if (ajalugu.Count < MinMuuke || (tana - alg).Days < MinPaevi) return VahaAndmeid(s);

        var paevane = ajalugu.GroupBy(m => m.Aeg.Date).ToDictionary(g => g.Key, g => (double)g.Sum(m => m.Kogus));
        var paevad = new List<(DateTime Paev, double Q, Pyha? Tahtpaev)>();
        for (var d = alg; d < tana; d = d.AddDays(1))
            paevad.Add((d, paevane.TryGetValue(d, out var q) ? q : 0, Pyhad.Aktiivne(d)));

        var tavalised = paevad.Where(x => x.Tahtpaev == null).ToList();
        if (tavalised.Count < 14) return VahaAndmeid(s);
        double baas0 = tavalised.Average(x => x.Q);
        if (baas0 <= 1e-9) return VahaAndmeid(s);

        // Nädalapäeva tegurid (tavalised päevad, silutud kahe "lisapäevaga")
        var nadal = new double[7];
        for (int w = 0; w < 7; w++)
        {
            var osa = tavalised.Where(x => (int)x.Paev.DayOfWeek == w).ToList();
            nadal[w] = (osa.Sum(x => x.Q) + 2 * baas0) / ((osa.Count + 2) * baas0);
        }
        double keskmine = nadal.Average();
        for (int w = 0; w < 7; w++) nadal[w] /= keskmine;

        // Nädalapäevast puhastatud baastase; värskemad päevad kaaluvad rohkem (poolestusaeg 90 päeva)
        var des = tavalised.Select(x => (x.Paev, Des: x.Q / nadal[(int)x.Paev.DayOfWeek])).ToList();
        double kaal = 0, kaalutud = 0, summa = 0;
        foreach (var x in des)
        {
            double w = Math.Pow(0.5, (tana - x.Paev).Days / 90.0);
            kaal += w; kaalutud += x.Des * w; summa += x.Des;
        }
        double baas = kaalutud / kaal, baasKoik = summa / des.Count;
        double suund = baasKoik > 1e-9 ? baas / baasKoik : 1;

        // Pühade tegurid: tegelik müük püha aknas võrreldes lähiümbruse tavapäevadega
        var pyhaTegur = new Dictionary<string, double>();
        foreach (var grupp in paevad.Where(x => x.Tahtpaev != null).GroupBy(x => x.Tahtpaev!.Nimi))
        {
            var read = grupp.ToList();
            if (read.Count < 3) continue;
            double tegelik = 0, oodatud = 0;
            foreach (var x in read)
            {
                var lahedal = des.Where(y => Math.Abs((y.Paev - x.Paev).Days) <= 45).ToList();
                double lokaalne = lahedal.Count > 0 ? lahedal.Average(y => y.Des) : baasKoik;
                tegelik += x.Q; oodatud += lokaalne * nadal[(int)x.Paev.DayOfWeek];
            }
            pyhaTegur[grupp.Key] = Math.Clamp((tegelik + baasKoik) / (oodatud + baasKoik), 0.3, 4.0);
        }

        // Tunnijaotus
        var tunnid = new double[24];
        foreach (var m in ajalugu) tunnid[m.Aeg.Hour] += m.Kogus;
        double tunnidKokku = tunnid.Sum();
        if (tunnidKokku <= 0) for (int h = 8; h <= 21; h++) tunnid[h] = 1.0 / 14;
        else for (int h = 0; h < 24; h++) tunnid[h] /= tunnidKokku;
        double Jaanud(int tund) { double r = 0; for (int h = Math.Max(0, tund); h < 24; h++) r += tunnid[h]; return r; }

        double Prognoos(DateTime paev, double osa)
        {
            var p = Pyhad.Aktiivne(paev);
            double t = p != null && pyhaTegur.TryGetValue(p.Nimi, out var f) ? f : 1;
            return baas * nadal[(int)paev.DayOfWeek] * t * osa;
        }

        double noudlus = 0, jaak = toode.Kogus; double? katab = null;
        double tanaOsa = Jaanud(nuud.Hour);
        double tanaVeel = Prognoos(tana, tanaOsa);
        for (int i = 0; i < horisont; i++)
        {
            double dem = Prognoos(tana.AddDays(i), i == 0 ? tanaOsa : 1);
            noudlus += dem;
            if (katab == null && dem > 1e-9)
            {
                if (jaak <= dem) katab = i + jaak / dem;
                else jaak -= dem;
            }
        }
        if (toode.Kogus <= 0) katab = 0;

        int tippPaev = Array.IndexOf(nadal, nadal.Max());
        int tippAlgus = 0; double parim = -1;
        for (int h = 0; h <= 21; h++)
        {
            double sum = tunnid[h] + tunnid[h + 1] + tunnid[h + 2];
            if (sum > parim) { parim = sum; tippAlgus = h; }
        }

        double vajadus = noudlus * Varu - toode.Kogus;
        s.Telli = vajadus > 0 ? (int)Math.Ceiling(vajadus) : 0;
        s.Aste = toode.Kogus <= 0 || katab is < 3 ? Tahtsus.Kriitiline : s.Telli > 0 ? Tahtsus.Telli : Tahtsus.Piisav;
        s.OnAndmeid = true; s.Katab = katab; s.Noudlus = noudlus; s.TanaVeel = tanaVeel;

        var pyhad = Enumerable.Range(0, horisont).Select(i => Pyhad.Aktiivne(tana.AddDays(i)))
            .Where(p => p != null).Select(p => p!.Nimi).Distinct()
            .Where(n => pyhaTegur.ContainsKey(n)).Select(n => (Nimi: n, Tegur: pyhaTegur[n]))
            .Where(x => Math.Abs(x.Tegur - 1) >= 0.2).OrderByDescending(x => x.Tegur).Take(2);
        var osad = new List<string>();
        foreach (var x in pyhad) osad.Add($"{x.Nimi} {(x.Tegur - 1).ToString("+0%;-0%")}");
        osad.Add($"tipp: {PaevaNimed[tippPaev]} kl {tippAlgus}–{tippAlgus + 3}");
        s.Pohjendus = string.Join(" · ", osad);
        s.Trend = suund > 1.08 || suund < 0.92 ? $"{(suund > 1 ? "↑" : "↓")} {(suund - 1).ToString("+0%;-0%")}" : "→ stabiilne";
        return s;
    }

    private static Soovitus VahaAndmeid(Soovitus s)
    {
        int k = s.Toode.Kogus;
        s.OnAndmeid = false;
        s.Aste = k <= 0 ? Tahtsus.Kriitiline : k <= Ladu.LoppemiseLavi ? Tahtsus.Telli : Tahtsus.AndmeidVahe;
        s.Katab = k <= 0 ? 0 : null;
        s.Pohjendus = k <= 0 ? "Otsas · müügiajalugu puudub, otsusta ise"
            : k <= Ladu.LoppemiseLavi ? "Lõppemas · müügiajalugu puudub"
            : "Müügiajalugu on liiga lühike";
        s.Trend = "—";
        return s;
    }
}
