namespace Tooded_EFCore;

internal static class ProductPhotos
{
    private static readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Värsked apelsinid"] = "oranges", ["Rohelised õunad"] = "apples",
        ["Avokaado"] = "avocado", ["Värsked tomatid"] = "tomatoes",
        ["Apelsinimahl"] = "juice", ["Mineraalvesi"] = "water",
        ["Tume šokolaad"] = "chocolate", ["Võiküpsised"] = "cookies"
    };
    internal static Image? Load(Toode product)
    {
        if (!files.TryGetValue(product.Toodenimetus, out var file)) return null;
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Products", file + ".jpg");
        if (!File.Exists(path)) return null;
        try { using var image = Image.FromFile(path); return new Bitmap(image); }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException) { return null; }
    }
}
