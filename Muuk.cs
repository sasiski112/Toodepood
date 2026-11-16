namespace Tooded_EFCore;

// Üks müügirida: millal ja mitu ühikut toodet osteti. Sellest arvutatakse ostusoovitused.
public class Muuk
{
    public int Id { get; set; }
    public int ToodeId { get; set; }
    public Toode Toode { get; set; } = null!;
    public int Kogus { get; set; }
    public DateTime Aeg { get; set; }
    public bool Demo { get; set; }          // true = demoandmed, false = päris ost
}
