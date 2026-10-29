namespace Tooded_EFCore;

// Sisseloginud kasutaja andmed, kättesaadavad igast vormist
public static class AktiivneKasutaja
{
    public static string Nimi { get; set; } = "";
    public static string Roll { get; set; } = "";
    public static bool OnAdmin => Roll == "admin";
}
