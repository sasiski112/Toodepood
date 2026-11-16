namespace Tooded_EFCore;

internal enum LaoSeis { Otsas, Loppemas, Olemas }

// Ühtne reegel, mille järgi tooteid "otsas" ja "lõppemas" värvitakse
internal static class Ladu
{
    internal const int LoppemiseLavi = 5;   // kuni nii mitu tükki loetakse "lõppemaks"

    internal static readonly Color OtsasTaust = Color.FromArgb(253, 232, 229);
    internal static readonly Color OtsasTekst = Color.FromArgb(178, 48, 38);
    internal static readonly Color LoppemasTaust = Color.FromArgb(255, 244, 214);
    internal static readonly Color LoppemasTekst = Color.FromArgb(158, 98, 8);
    internal static readonly Color LoppemasSilt = Color.FromArgb(214, 140, 20);

    internal static LaoSeis Seis(int kogus) =>
        kogus <= 0 ? LaoSeis.Otsas : kogus <= LoppemiseLavi ? LaoSeis.Loppemas : LaoSeis.Olemas;

    internal static string Tekst(int kogus) => Seis(kogus) switch
    {
        LaoSeis.Otsas => "Otsas",
        LaoSeis.Loppemas => "Lõppemas",
        _ => "Laos"
    };
}
