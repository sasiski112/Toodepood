using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

public partial class Form2 : Form
{
    private readonly DataGridView grid = new();
    private readonly TextBox search = new() { PlaceholderText = "Otsi toodet…" };
    private readonly TextBox name = new();
    private readonly NumericUpDown stock = new() { Maximum = 1000000 };
    private readonly NumericUpDown price = new() { Maximum = 99999999, DecimalPlaces = 2, Increment = 0.10m };
    private readonly ComboBox category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly PictureBox picture = new() { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Ui.Background };
    private readonly Label status = Ui.Label("", 10, Ui.Muted);
    private int? selected;
    private string? imagePath;
    public Form2()
    {
        InitializeComponent(); Ui.Setup(this, "Tootehaldus", 1000, 780);
        var header = new Panel { Dock = DockStyle.Top, Height = 170 };
        var editor = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, AutoScroll = true };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 20, 24) };
        Controls.Add(body); Controls.Add(editor); Controls.Add(header);
        Ui.Place(header, Ui.Label("TÖÖLAUD  /  ADMINISTRAATOR", 9, Ui.Muted, true), 28, 18, 600, 26);
        Ui.Place(header, Ui.Label("Tootehaldus", 27, bold: true), 26, 51, 700, 54);
        Ui.Place(header, search, 28, 119, 570, 32);
        grid.Dock = DockStyle.Fill; Ui.Grid(grid); body.Controls.Add(grid);
        Ui.Place(editor, Ui.Label("Toote andmed", 18, bold: true), 20, 18, 260, 40);
        Field(editor, "Toote nimetus", name, 70);
        Field(editor, "Laoseis · tk", stock, 143);
        Field(editor, "Hind · €", price, 216);
        Field(editor, "Kategooria", category, 289);
        Ui.Place(editor, picture, 20, 366, 88, 75);
        Ui.Place(editor, Ui.Button("Vali pilt…", ChooseImage, false), 123, 383, 155, 40);
        Ui.Place(editor, Ui.Button("Salvesta toode", Save), 20, 461, 258, 42);
        Ui.Place(editor, Ui.Button("+  Uus toode", (_, _) => Reset(), false), 20, 513, 125, 40);
        Ui.Place(editor, Ui.Button("Kustuta", Delete, false), 153, 513, 125, 40);
        Ui.Place(editor, status, 20, 567, 260, 60);
        using var db = new ToodedContext();
        category.DisplayMember = "Kategooria_nimetus"; category.ValueMember = "Id";
        category.DataSource = db.Kategooriatabel.OrderBy(k => k.Kategooria_nimetus).ToList();
        grid.SelectionChanged += (_, _) => SelectProduct();
        search.TextChanged += (_, _) => LoadProducts();
        editor.Enabled = AktiivneKasutaja.OnAdmin;
        LoadProducts(); Reset();
    }
    private static void Field(Control parent, string label, Control input, int y)
    { Ui.Place(parent, Ui.Label(label, 10, Ui.Muted), 20, y, 258, 25); Ui.Place(parent, input, 20, y + 29, 258, 32); }
    private void LoadProducts()
    {
        using var db = new ToodedContext();
        var query = db.Toodetabel.AsNoTracking().Include(p => p.Kategooria).ToList()
            .Where(p => (p.Toodenimetus.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || L.T(p.Toodenimetus).Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)));
        grid.DataSource = query.Select(p => new { p.Id, Toode = p.Toodenimetus, Kategooria = p.Kategooria.Kategooria_nimetus, Laos = p.Kogus, Hind = p.Hind }).ToList();
        grid.Columns["Id"].Visible = false;
        grid.Columns["Hind"].DefaultCellStyle.Format = "0.00 €";
        grid.Columns["Laos"].FillWeight = 45; grid.Columns["Hind"].FillWeight = 55;
    }
    private void SelectProduct()
    {
        if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Cells["Id"].Value is not int id) return;
        using var db = new ToodedContext(); var p = db.Toodetabel.Find(id);
        if (p == null) return;
        selected = id; name.Text = p.Toodenimetus; stock.Value = Math.Clamp(p.Kogus, 0, 1000000);
        price.Value = Math.Clamp(p.Hind, 0, price.Maximum); category.SelectedValue = p.KategooriaId;
        imagePath = null; picture.Image?.Dispose(); picture.Image = Kaustad.LaePilt(p.Pilt) ?? ProductPhotos.Load(p);
        status.Text = $"Muudad toodet #{id}";
    }
    private void Reset()
    {
        grid.ClearSelection(); selected = null; name.Clear(); stock.Value = 0; price.Value = 0;
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
        if (string.IsNullOrWhiteSpace(name.Text) || name.Text.Trim().Length > 120 || category.SelectedValue is not int)
        { status.Text = "Sisesta nimi (1–120 märki) ja vali kategooria."; return; }
        try
        {
            using var db = new ToodedContext();
            var p = selected.HasValue ? db.Toodetabel.Find(selected.Value) : new Toode();
            if (p == null) { status.Text = "Toodet enam ei ole."; return; }
            p.Toodenimetus = name.Text.Trim(); p.Kogus = (int)stock.Value; p.Hind = price.Value;
            p.KategooriaId = (int)category.SelectedValue!;
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


