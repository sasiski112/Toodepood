using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

public partial class Form2 : Form
{
    private readonly DataGridView grid = new();
    private readonly TextBox search = new() { PlaceholderText = "Otsi toodet…" };
    private readonly TextBox name = new() { MaxLength = 100 };
    private readonly TextBox description = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, MaxLength = 1000 };
    private readonly NumericUpDown stock = new() { Maximum = 1000000 };
    private readonly NumericUpDown price = new() { Maximum = 99999999.99m, DecimalPlaces = 2, Increment = 0.10m };
    private readonly ComboBox category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly PictureBox picture = new() { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Ui.Background };
    private readonly Label status = Ui.Label("", 10, Ui.Muted);
    private readonly Label alerts = Ui.Label("", 10, Ui.Muted, true);
    private int? selected;
    private string? imagePath;
    public Form2()
    {
        InitializeComponent(); Ui.Setup(this, "Tootehaldus", 1000, 780);
        var header = new Panel { Dock = DockStyle.Top, Height = 170 };
        var editor = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White };
        var fields = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var actions = new Panel { Dock = DockStyle.Bottom, Height = 150 };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 20, 24) };
        Controls.Add(body); Controls.Add(editor); Controls.Add(header);
        editor.Controls.Add(fields); editor.Controls.Add(actions);
        Ui.Place(header, Ui.Label("TÖÖLAUD  /  ADMINISTRAATOR", 9, Ui.Muted, true), 28, 18, 600, 26);
        Ui.Place(header, Ui.Label("Tootehaldus", 27, bold: true), 26, 51, 700, 54);
        Ui.Place(header, search, 28, 119, 540, 32);
        Ui.Place(header, alerts, 580, 119, 190, 32);
        alerts.Anchor = AnchorStyles.Top | AnchorStyles.Right; alerts.TextAlign = ContentAlignment.MiddleRight;
        grid.Dock = DockStyle.Fill; Ui.Grid(grid); body.Controls.Add(grid);

        Ui.Place(fields, Ui.Label("Toote andmed", 18, bold: true), 20, 10, 260, 34);
        Field(fields, "Toote nimetus", name, 46);
        Field(fields, "Laoseis · tk", stock, 106);
        Field(fields, "Hind · €", price, 166);
        Field(fields, "Kategooria", category, 226);
        var addCategory = Ui.Button("+ Uus", AddCategory, false);
        addCategory.Font = new Font("Segoe UI", 9, FontStyle.Bold); addCategory.SetBounds(172, 224, 106, 26); fields.Controls.Add(addCategory);
        Ui.Place(fields, Ui.Button("Kustuta kategooria", DeleteCategory, false), 20, 286, 258, 32);
        Ui.Place(fields, picture, 20, 336, 88, 75);
        Ui.Place(fields, Ui.Button("Vali pilt…", ChooseImage, false), 123, 351, 155, 40);
        Ui.Place(fields, Ui.Label("Kirjeldus", 10, Ui.Muted), 20, 422, 258, 22);
        Ui.Place(fields, description, 20, 446, 258, 80);

        Ui.Place(actions, Ui.Button("Salvesta toode", Save), 20, 8, 258, 42);
        Ui.Place(actions, Ui.Button("+  Uus toode", (_, _) => Reset(), false), 20, 58, 125, 38);
        Ui.Place(actions, Ui.Button("Kustuta", Delete, false), 153, 58, 125, 38);
        Ui.Place(actions, status, 20, 102, 260, 40);

        grid.CellFormatting += HighlightRows;
        LoadCategories();
        grid.SelectionChanged += (_, _) => SelectProduct();
        search.TextChanged += (_, _) => LoadProducts();
        editor.Enabled = AktiivneKasutaja.OnAdmin;
        LoadProducts(); Reset();
    }
    private static void Field(Control parent, string label, Control input, int y)
    { Ui.Place(parent, Ui.Label(label, 10, Ui.Muted), 20, y, 150, 22); Ui.Place(parent, input, 20, y + 24, 258, 30); }
    private void LoadCategories(int? select = null)
    {
        using var db = new ToodedContext();
        category.DisplayMember = "Kategooria_nimetus"; category.ValueMember = "Id";
        category.DataSource = db.Kategooriatabel.OrderBy(k => k.Kategooria_nimetus).ToList();
        if (select.HasValue) category.SelectedValue = select.Value;
    }
    private void AddCategory(object? sender, EventArgs e)
    {
        if (!AktiivneKasutaja.OnAdmin) return;
        using var dialog = new KategooriaDialog();
        if (dialog.ShowDialog(TopLevelControl) != DialogResult.OK || dialog.Loodud is not { } uus) return;
        LoadCategories(uus.Id); status.Text = $"Kategooria „{uus.Kategooria_nimetus}” lisatud.";
    }
    private void DeleteCategory(object? sender, EventArgs e)
    {
        if (!AktiivneKasutaja.OnAdmin || category.SelectedValue is not int id) return;
        if (MessageBox.Show(this, $"Kustuta kategooria „{category.Text}”?", "Tooded",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new ToodedContext();
            KategooriaTeenus.Kustuta(db, id);
            LoadCategories();
            status.Text = "Kategooria kustutatud.";
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }
    // Otsasolevad tooted punaselt, lõppevad kollaselt
    private void HighlightRows(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle == null || e.RowIndex < 0 || !grid.Columns.Contains("Laos")) return;
        if (grid.Rows[e.RowIndex].Cells["Laos"].Value is not int laos) return;
        var seis = Ladu.Seis(laos);
        if (seis == LaoSeis.Olemas) return;
        e.CellStyle.BackColor = seis == LaoSeis.Otsas ? Ladu.OtsasTaust : Ladu.LoppemasTaust;
        e.CellStyle.ForeColor = seis == LaoSeis.Otsas ? Ladu.OtsasTekst : Ladu.LoppemasTekst;
        e.CellStyle.SelectionBackColor = seis == LaoSeis.Otsas ? Color.FromArgb(246, 205, 199) : Color.FromArgb(252, 230, 170);
        e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
    }
    private void LoadProducts()
    {
        using var db = new ToodedContext();
        var all = db.Toodetabel.AsNoTracking().Include(p => p.Kategooria).ToList();
        int otsas = all.Count(p => Ladu.Seis(p.Kogus) == LaoSeis.Otsas), loppemas = all.Count(p => Ladu.Seis(p.Kogus) == LaoSeis.Loppemas);
        alerts.Text = otsas + loppemas == 0 ? "Laoseis korras" : $"Otsas: {otsas}  ·  Lõppemas: {loppemas}";
        alerts.ForeColor = otsas > 0 ? Ladu.OtsasTekst : loppemas > 0 ? Ladu.LoppemasTekst : Ui.Muted;
        var query = all.Where(p => (p.Toodenimetus.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || L.T(p.Toodenimetus).Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)));
        grid.DataSource = query.Select(p => new { p.Id, Toode = p.Toodenimetus, Kategooria = p.Kategooria.Kategooria_nimetus, Laos = p.Kogus, Seis = Ladu.Tekst(p.Kogus), Hind = p.Hind }).ToList();
        grid.Columns["Id"].Visible = false;
        grid.Columns["Hind"].DefaultCellStyle.Format = "0.00 €";
        grid.Columns["Laos"].FillWeight = 40; grid.Columns["Seis"].FillWeight = 55; grid.Columns["Hind"].FillWeight = 50;
    }
    private void SelectProduct()
    {
        if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Cells["Id"].Value is not int id) return;
        using var db = new ToodedContext(); var p = db.Toodetabel.Find(id);
        if (p == null) return;
        selected = id; name.Text = p.Toodenimetus; stock.Value = Math.Clamp(p.Kogus, 0, 1000000);
        price.Value = Math.Clamp(p.Hind, 0, price.Maximum); category.SelectedValue = p.KategooriaId;
        description.Text = p.Kirjeldus ?? "";
        imagePath = null; picture.Image?.Dispose(); picture.Image = Kaustad.LaePilt(p.Pilt) ?? ProductPhotos.Load(p);
        status.Text = $"Muudad toodet #{id}";
    }
    private void Reset()
    {
        grid.ClearSelection(); selected = null; name.Clear(); description.Clear(); stock.Value = 0; price.Value = 0;
        imagePath = null; picture.Image?.Dispose(); picture.Image = null; status.Text = "Uue toote lisamine"; name.Focus();
    }
    private void ChooseImage(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = L.T("Pildid|*.jpg;*.jpeg;*.png") };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            using var source = Image.FromFile(dialog.FileName); var copy = new Bitmap(source);
            picture.Image?.Dispose(); picture.Image = copy; imagePath = dialog.FileName;
        }
        catch { status.Text = "Pildi avamine ebaõnnestus."; }
    }
    private void Save(object? sender, EventArgs e)
    {
        if (!AktiivneKasutaja.OnAdmin) return;
        if (string.IsNullOrWhiteSpace(name.Text) || name.Text.Trim().Length > 100 || category.SelectedValue is not int)
        { status.Text = "Sisesta nimi (1–100 märki) ja vali kategooria."; return; }
        try
        {
            using var db = new ToodedContext();
            var p = selected.HasValue ? db.Toodetabel.Find(selected.Value) : new Toode();
            if (p == null) { status.Text = "Toodet enam ei ole."; return; }
            p.Toodenimetus = name.Text.Trim(); p.Kogus = (int)stock.Value; p.Hind = price.Value;
            p.KategooriaId = (int)category.SelectedValue!;
            p.Kirjeldus = string.IsNullOrWhiteSpace(description.Text) ? null : description.Text.Trim();
            if (imagePath != null)
            {
                p.Pilt = Guid.NewGuid().ToString("N") + Path.GetExtension(imagePath);
                File.Copy(imagePath, Path.Combine(Kaustad.Pildid, p.Pilt));
            }
            if (!selected.HasValue) db.Toodetabel.Add(p);
            db.SaveChanges(); LoadProducts(); Reset(); status.Text = "Toode on salvestatud.";
        }
        catch (Exception ex) { status.Text = "Salvestamine ebaõnnestus."; MessageBox.Show(ex.Message, "Tooded"); }
    }
    private void Delete(object? sender, EventArgs e)
    {
        if (!AktiivneKasutaja.OnAdmin || selected is not int id) return;
        if (MessageBox.Show(L.T($"Kustuta „{name.Text}”?"), L.T("Kustutamine"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            using var db = new ToodedContext(); var p = db.Toodetabel.Find(id);
            if (p != null) { db.Remove(p); db.SaveChanges(); }
            Ostukorv.Ridad.RemoveAll(r => r.Toode.Id == id);
            LoadProducts(); Reset(); status.Text = "Toode kustutatud.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, L.T("Kustutamine ebaõnnestus")); }
    }
    protected override void OnFormClosed(FormClosedEventArgs e) { picture.Image?.Dispose(); base.OnFormClosed(e); }
}
