namespace Tooded_EFCore;

internal static class DemoData
{
    internal static void Seed(ToodedContext db)
    {
        db.Toodetabel.AddRange(
            new Toode { Toodenimetus = "Värsked apelsinid", Hind = 2.90m, Kogus = 24, KategooriaId = 2 },
            new Toode { Toodenimetus = "Rohelised õunad", Hind = 1.80m, Kogus = 36, KategooriaId = 2 },
            new Toode { Toodenimetus = "Avokaado", Hind = 2.40m, Kogus = 12, KategooriaId = 2 },
            new Toode { Toodenimetus = "Värsked tomatid", Hind = 3.20m, Kogus = 18, KategooriaId = 2 },
            new Toode { Toodenimetus = "Apelsinimahl", Hind = 3.50m, Kogus = 16, KategooriaId = 1 },
            new Toode { Toodenimetus = "Mineraalvesi", Hind = 1.20m, Kogus = 48, KategooriaId = 1 },
            new Toode { Toodenimetus = "Tume šokolaad", Hind = 2.60m, Kogus = 20, KategooriaId = 3 },
            new Toode { Toodenimetus = "Võiküpsised", Hind = 3.90m, Kogus = 0, KategooriaId = 3 });
        db.SaveChanges();
    }
}
