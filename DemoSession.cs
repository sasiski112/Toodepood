namespace Tooded_EFCore;

// Külalisseanss kasutab päris poevaadet, kuid ei loo kontot ega müüke.
internal sealed class DemoSession : IDisposable
{
    private readonly string name;
    private readonly string role;
    private readonly OstukorviRida[] cart;
    private bool disposed;
    private readonly Receipt? receipt;

    internal DemoSession()
    {
        if (!AktiivneKasutaja.OnAdmin || AktiivneKasutaja.OnDemo)
            throw new InvalidOperationException("Demo saab avada ainult administraator.");
        receipt = Receipt.Last; Receipt.Last = null;
        name = AktiivneKasutaja.Nimi;
        role = AktiivneKasutaja.Roll;
        cart = Ostukorv.Ridad.ToArray();
        Ostukorv.Tyhjenda();
        AktiivneKasutaja.Nimi = "Demo klient";
        AktiivneKasutaja.Roll = "kasutaja";
        AktiivneKasutaja.OnDemo = true;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Receipt.Last = receipt;
        Ostukorv.Tyhjenda();
        Ostukorv.Ridad.AddRange(cart);
        AktiivneKasutaja.Nimi = name;
        AktiivneKasutaja.Roll = role;
        AktiivneKasutaja.OnDemo = false;
    }
}
