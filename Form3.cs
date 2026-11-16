using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

public partial class Form3 : Form
{
    private readonly FlowLayoutPanel cards = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(22, 8, 10, 10) };
    private readonly TextBox search = new() { PlaceholderText = "Otsi toote nime järgi…" };
    private readonly ComboBox sort = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label count = Ui.Label("", 10, Ui.Muted);
    private readonly Label cart = Ui.Label("", 10, Ui.Green, true);
    private readonly List<Button> categories = new();
    private readonly List<Toode> products;
    private int category;
    public Form3()
    {
        InitializeComponent(); Ui.Setup(this, "Pood", 1000, 780);
        using var db = new ToodedContext();
        products = db.Toodetabel.AsNoTracking().Include(t => t.Kategooria).ToList();
        var top = new Panel { Dock = DockStyle.Top, Height = 292 };
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(28, 0, 28, 0) };
        Controls.Add(cards); Controls.Add(top); Controls.Add(footer);
        cart.Dock = DockStyle.Fill; footer.Controls.Add(cart);
        Ui.Place(top, Ui.Label("POOD  /  VÄRSKE VALIK", 9, Ui.Muted, true), 28, 18, 500, 26);
        Ui.Place(top, Ui.Label("Midagi head igasse päeva.", 27, bold: true), 26, 51, 900, 54);
        Ui.Place(top, Ui.Label(AktiivneKasutaja.OnAdmin ? "Administraatori vaade: ostmine pole saadaval. Punane = otsas, kollane = lõppemas." : "Leia oma lemmikud ja pane ostukorv kokku paari klõpsuga.", 11, Ui.Muted), 28, 109, 850, 30);
        Ui.Place(top, search, 28, 158, 590, 32);
        search.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        sort.Items.AddRange(new object[] { "Nime järgi", "Hind: odavam enne", "Hind: kallim enne" });
        sort.SelectedIndex = 0;
        Ui.Place(top, sort, 640, 158, 330, 32);
        sort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var filters = new FlowLayoutPanel { WrapContents = false, AutoScroll = true };
        Ui.Place(top, filters, 24, 210, 946, 49); filters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        AddCategory(filters, "Kõik tooted", 0);
        foreach (var item in db.Kategooriatabel.OrderBy(k => k.Id)) AddCategory(filters, item.Kategooria_nimetus, item.Id);
        Ui.Place(top, count, 28, 261, 900, 24);
        search.TextChanged += (_, _) => Render(); sort.SelectedIndexChanged += (_, _) => Render();
        Render();
    }
    private void AddCategory(FlowLayoutPanel parent, string title, int id)
    {
        var button = Ui.Button(title, (_, _) => { category = id; Render(); }, false);
        button.Tag = id; button.AutoSize = true; button.Padding = new Padding(12, 0, 12, 0); button.Height = 36;
        categories.Add(button); parent.Controls.Add(button);
    }
    private void Render()
    {
        cards.SuspendLayout();
        foreach (Control child in cards.Controls.Cast<Control>().ToArray()) child.Dispose();
        foreach (var button in categories)
        { button.BackColor = (int)button.Tag! == category ? Ui.Green : Color.White; button.ForeColor = (int)button.Tag! == category ? Color.White : Ui.Ink; }
        var filtered = products.Where(p => (category == 0 || p.KategooriaId == category) && (p.Toodenimetus.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || L.T(p.Toodenimetus).Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)));
        filtered = sort.SelectedIndex switch { 1 => filtered.OrderBy(p => p.Hind), 2 => filtered.OrderByDescending(p => p.Hind), _ => filtered.OrderBy(p => p.Toodenimetus) };
        var list = filtered.ToList(); count.Text = $"{list.Count} toodet  ·  Hinnad eurodes";
        if (AktiivneKasutaja.OnAdmin)
        {
            int otsas = products.Count(p => Ladu.Seis(p.Kogus) == LaoSeis.Otsas), loppemas = products.Count(p => Ladu.Seis(p.Kogus) == LaoSeis.Loppemas);
            if (otsas + loppemas > 0) count.Text += $"  ·  otsas: {otsas}  ·  lõppemas: {loppemas}";
        }
        foreach (var product in list) cards.Controls.Add(Card(product));
        if (list.Count == 0) cards.Controls.Add(new Label { Text = "Tooteid ei leitud. Proovi teist otsingut või kategooriat.", AutoSize = true, Padding = new Padding(10, 35, 0, 0) });
        cards.ResumeLayout(); UpdateCart();
    }
    private Panel Card(Toode product)
    {
        var seis = Ladu.Seis(product.Kogus);
        var panel = new Panel
        {
            BackColor = seis == LaoSeis.Otsas ? Ladu.OtsasTaust : seis == LaoSeis.Loppemas ? Ladu.LoppemasTaust : Color.White,
            Size = new Size(218, 310), Margin = new Padding(6, 6, 10, 14)
        };
        var art = new ProductArt(product) { Tuhm = seis == LaoSeis.Otsas };
        if (seis == LaoSeis.Otsas) { art.Silt = "OTSAS"; art.SiltTaust = Ladu.OtsasTekst; }
        else if (seis == LaoSeis.Loppemas) { art.Silt = $"LÕPPEMAS · {product.Kogus} tk"; art.SiltTaust = Ladu.LoppemasSilt; }
        Ui.Place(panel, art, 0, 0, 218, 155);
        Ui.Place(panel, Ui.Label(product.Kategooria.Kategooria_nimetus.ToUpperInvariant(), 8, Ui.Muted), 14, 164, 192, 20);
        Ui.Place(panel, Ui.Label(product.Toodenimetus, 12, bold: true), 14, 186, 192, 32);
        Ui.Place(panel, Ui.Label($"{product.Hind:F2} €", 17, bold: true), 14, 221, 100, 32);
        var stockColor = seis == LaoSeis.Otsas ? Ladu.OtsasTekst : seis == LaoSeis.Loppemas ? Ladu.LoppemasTekst : Ui.Muted;
        Ui.Place(panel, Ui.Label(product.Kogus > 0 ? $"Laos: {product.Kogus}" : "Otsas", 9, stockColor, seis != LaoSeis.Olemas), 120, 222, 90, 30);
        if (AktiivneKasutaja.OnAdmin)
            Ui.Place(panel, Ui.Button("Vaata kirjeldust", (_, _) => OpenDetails(product), false), 14, 261, 190, 36);
        else
        {
            var quantity = new NumericUpDown { Minimum = 1, Maximum = Math.Max(1, product.Kogus), Value = 1, Enabled = product.Kogus > 0 };
            Ui.Place(panel, quantity, 14, 267, 57, 29);
            var add = Ui.Button("+  Lisa korvi", (_, _) =>
            {
                if (product.Kogus <= 0) { Dialoogid.Vabandus(TopLevelControl, product.Toodenimetus); return; }
                if (!Ostukorv.Lisa(product, (int)quantity.Value)) { cart.Text = $"Laos on {product.Kogus} tk; korvis juba {Ostukorv.KogusKorvis(product.Id)} tk."; return; }
                UpdateCart(product.Toodenimetus + " lisatud.   ");
            }, product.Kogus > 0);
            Ui.Place(panel, add, 79, 261, 125, 36);
        }
        MakeClickable(panel, product);
        return panel;
    }
    // Klõps tootekaardil (v.a. nupud ja kogusevalija) avab toote täieliku kirjelduse
    private void MakeClickable(Control control, Toode product)
    {
        control.Cursor = Cursors.Hand; control.Click += (_, _) => OpenDetails(product);
        foreach (Control child in control.Controls)
            if (child is not Button && child is not NumericUpDown) MakeClickable(child, product);
    }
    private void OpenDetails(Toode product)
    {
        using var dialog = new ToodeDetailForm(product, !AktiivneKasutaja.OnAdmin);
        dialog.ShowDialog(TopLevelControl);
        if (dialog.Lisatud > 0) UpdateCart(product.Toodenimetus + " lisatud.   ");
    }
    private void UpdateCart(string prefix = "") => cart.Text = AktiivneKasutaja.OnAdmin
        ? "Administraator ei saa osta."
        : $"{prefix}Ostukorvis: {Ostukorv.Ridad.Sum(r => r.Kogus)} toodet   ·   {Ostukorv.Kokku:F2} €";
}
