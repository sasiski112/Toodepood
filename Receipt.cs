namespace Tooded_EFCore;

internal sealed record ReceiptLine(string Name, int Quantity, decimal Price)
{
    internal decimal Total => Quantity * Price;
}

internal sealed class Receipt
{
    internal static Receipt? Last { get; set; }
    internal string Number { get; } = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
    internal DateTime Created { get; } = DateTime.Now;
    internal bool Demo { get; }
    internal IReadOnlyList<ReceiptLine> Lines { get; }
    internal decimal Total => Lines.Sum(l => l.Total);
    internal string? PdfPath { get; set; }
    internal Receipt(IEnumerable<OstukorviRida> rows, bool demo)
    {
        Demo = demo;
        Lines = Array.AsReadOnly(rows.Select(r => new ReceiptLine(r.Toode.Toodenimetus, r.Kogus, r.Toode.Hind)).ToArray());
    }
}
