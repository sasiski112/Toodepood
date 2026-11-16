using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

// Administraatori ostusoovitused: prognoos müügiajaloost (nädalapäev, kellaaeg, pühad)
internal sealed class SoovitusedForm : Form
{
    private readonly DataGridView grid = new();
    private readonly ComboBox horizon = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label holidays = Ui.Label("", 10, Ui.Ink);
    private readonly Label info = Ui.Label("", 9, Ui.Muted);
    private readonly Button clearDemo;
    private static readonly int[] Horisondid = { 7, 14, 30, 60 };

    internal SoovitusedForm()
    {
        Ui.Setup(this, "Ostusoovitused", 1000, 780);
        var header = new Panel { Dock = DockStyle.Top, Height = 176 };
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(28, 8, 20, 8) };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 20, 8) };
        Controls.Add(body); Controls.Add(footer); Controls.Add(header);
        Ui.Place(header, Ui.Label("TÖÖLAUD  /  LAOANALÜÜS", 9, Ui.Muted, true), 28, 18, 600, 26);
        Ui.Place(header, Ui.Label("Ostusoovitused", 27, bold: true), 26, 51, 700, 54);
        Ui.Place(header, Ui.Label("Prognoos arvestab nädalapäeva, kellaaega ja pühi.", 11, Ui.Muted), 28, 106, 620, 28);
        var label = Ui.Label("Horisont:", 10, Ui.Muted); Ui.Place(header, label, 690, 106, 70, 28); label.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        horizon.Items.AddRange(Horisondid.Select(d => (object)$"{d} päeva").ToArray()); horizon.SelectedIndex = 1;
        Ui.Place(header, horizon, 764, 104, 190, 30); horizon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Ui.Place(header, holidays, 28, 140, 926, 28); holidays.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        grid.Dock = DockStyle.Fill; Ui.Grid(grid); body.Controls.Add(grid);
        grid.CellFormatting += Highlight;
        info.Dock = DockStyle.Fill; footer.Controls.Add(info);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 380, FlowDirection = FlowDirection.RightToLeft };
        footer.Controls.Add(buttons);
        buttons.Controls.Add(Ui.Button("Uuenda", (_, _) => Reload()));
        clearDemo = Ui.Button("Eemalda demoajalugu", ClearDemo, false); clearDemo.Width = 190;
        buttons.Controls.Add(clearDemo);
        horizon.SelectedIndexChanged += (_, _) => Reload();
        Reload();
    }

    private void Reload()
    {
        try
        {
            int days = Horisondid[Math.Max(0, horizon.SelectedIndex)];
            using var db = new ToodedContext();
            var tooded = db.Toodetabel.AsNoTracking().ToList();
            var muugid = db.Muugid.AsNoTracking().Select(m => new MuukPunkt(m.ToodeId, m.Kogus, m.Aeg)).ToList();
            int demo = db.Muugid.Count(m => m.Demo);
            var now = DateTime.Now;
            var tulemused = Soovitused.Arvuta(tooded, muugid, now, days);

            grid.DataSource = tulemused.Select(s => new
            {
                Prioriteet = s.Aste switch { Tahtsus.Kriitiline => "Kriitiline", Tahtsus.Telli => "Telli", Tahtsus.Piisav => "Piisav", _ => "Andmeid vähe" },
                Toode = s.Toode.Toodenimetus,
                Laos = s.Toode.Kogus,
                Kestab = s.Toode.Kogus <= 0 ? "Otsas" : s.Katab is double k ? $"{k:0.#} p" : s.OnAndmeid ? $"{days}+ p" : "—",
                TanaVeel = s.OnAndmeid ? s.TanaVeel.ToString("0.0") : "—",
                Noudlus = s.OnAndmeid ? $"{s.Noudlus:0} tk" : "—",
                Telli = s.Telli is int n && n > 0 ? $"+{n} tk" : "—",
                Pohjendus = s.Pohjendus,
                Trend = s.Trend
            }).ToList();
            grid.Columns["TanaVeel"].HeaderText = "Täna veel";
            grid.Columns["Noudlus"].HeaderText = $"Nõudlus {days} p";
            grid.Columns["Telli"].HeaderText = "Soovitus";
            grid.Columns["Pohjendus"].HeaderText = "Põhjus";
            var weights = new Dictionary<string, float> { ["Prioriteet"] = 70, ["Toode"] = 110, ["Laos"] = 40, ["Kestab"] = 55, ["TanaVeel"] = 55, ["Noudlus"] = 70, ["Telli"] = 60, ["Pohjendus"] = 230, ["Trend"] = 80 };
            foreach (var w in weights) grid.Columns[w.Key].FillWeight = w.Value;

            var tulevased = Pyhad.Lahedased(now, 60).Take(3).Select(p => $"{p.Nimi} {p.Kuupaev:dd.MM} ({Kaugus(p.Kuupaev, now)})").ToList();
            holidays.Text = tulevased.Count == 0 ? "Lähimatel kuudel pühi ei ole." : "Lähimad pühad:   " + string.Join("   ·   ", tulevased);

            string period = muugid.Count > 0 ? $"; periood {muugid.Min(m => m.Aeg):dd.MM.yyyy}–{muugid.Max(m => m.Aeg):dd.MM.yyyy}" : "";
            info.Text = muugid.Count == 0
                ? "Müügiandmeid veel pole. Soovitused ilmuvad, kui klientide ostud on kinnitatud."
                : $"Müügiandmed: {muugid.Count} rida{(demo > 0 ? $" (sh demoajalugu {demo})" : "")}{period}. Päris ostud lisanduvad, kui klient ostu kinnitab.";
            clearDemo.Visible = demo > 0;
        }
        catch (Exception ex) { info.Text = "Soovituste arvutamine ebaõnnestus: " + ex.Message; }
    }

    private static string Kaugus(DateTime paev, DateTime now)
    {
        int n = (paev.Date - now.Date).Days;
        return n == 0 ? "täna" : n == 1 ? "homme" : $"{n} p pärast";
    }

    private void ClearDemo(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Eemalda kogu demoajalugu? Päris ostud jäävad alles ja soovitused arvutatakse ainult nende põhjal.", "Tooded",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { using var db = new ToodedContext(); db.Muugid.Where(m => m.Demo).ExecuteDelete(); }
        catch (Exception ex) { info.Text = "Eemaldamine ebaõnnestus: " + ex.Message; return; }
        Reload();
    }

    private void Highlight(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.CellStyle == null || e.RowIndex < 0 || !grid.Columns.Contains("Prioriteet")) return;
        switch (grid.Rows[e.RowIndex].Cells["Prioriteet"].Value as string)
        {
            case "Kriitiline":
                e.CellStyle.BackColor = Ladu.OtsasTaust; e.CellStyle.ForeColor = Ladu.OtsasTekst;
                e.CellStyle.SelectionBackColor = Color.FromArgb(246, 205, 199); e.CellStyle.SelectionForeColor = Ladu.OtsasTekst; break;
            case "Telli":
                e.CellStyle.BackColor = Ladu.LoppemasTaust; e.CellStyle.ForeColor = Ladu.LoppemasTekst;
                e.CellStyle.SelectionBackColor = Color.FromArgb(252, 230, 170); e.CellStyle.SelectionForeColor = Ladu.LoppemasTekst; break;
            case "Andmeid vähe": e.CellStyle.ForeColor = Ui.Muted; break;
        }
    }
}
