namespace Tooded_EFCore;

public static class Kaustad
{
    public static readonly string Juur = Environment.GetEnvironmentVariable("TOODED_DATA_DIR") ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToodedApp");

    public static readonly string Pildid = Path.Combine(Juur, "Images");
    public static readonly string Arved = Path.Combine(Juur, "Arved");

    // Loob kaustad, kui neid veel pole
    public static void Loo()
    {
        Directory.CreateDirectory(Pildid);
        Directory.CreateDirectory(Arved);
    }

    // Laeb pildi koopiana, et fail ei jääks lukku
    public static Image? LaePilt(string? failinimi)
    {
        if (string.IsNullOrEmpty(failinimi)) return null;
        string tee = Path.Combine(Pildid, failinimi);
        if (!File.Exists(tee)) return null;
        try
        {
            using var ajutine = Image.FromFile(tee);
            return new Bitmap(ajutine);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException) { return null; }
    }
}
