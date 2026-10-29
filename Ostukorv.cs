namespace Tooded_EFCore;

public class OstukorviRida
{
    public Toode Toode { get; set; } = null!;
    public int Kogus { get; set; }
    public decimal Summa => Toode.Hind * Kogus;

    public override string ToString() =>
        $"{Toode.Toodenimetus} – {Kogus} tk × {Toode.Hind:F2} € = {Summa:F2} €";
}

public static class Ostukorv
{
    public static List<OstukorviRida> Ridad { get; } = new();
    public static decimal Kokku => Ridad.Sum(r => r.Summa);

    public static int KogusKorvis(int toodeId) =>
        Ridad.FirstOrDefault(r => r.Toode.Id == toodeId)?.Kogus ?? 0;

    // Tagastab false, kui laos pole nii palju
    public static bool Lisa(Toode toode, int kogus)
    {
        if (kogus <= 0 || (long)KogusKorvis(toode.Id) + kogus > toode.Kogus) return false;

        var olemas = Ridad.FirstOrDefault(r => r.Toode.Id == toode.Id);
        if (olemas != null)
            olemas.Kogus += kogus;
        else
            Ridad.Add(new OstukorviRida { Toode = toode, Kogus = kogus });
        return true;
    }

    public static void Tyhjenda() => Ridad.Clear();
}
