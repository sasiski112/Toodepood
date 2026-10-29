using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Tooded_EFCore;

public partial class ArveForm : Form
{
    private readonly DataGridView grid = new();
    private readonly Label total = Ui.Label("", 28, bold: true);
    private readonly Label status = Ui.Label("", 10, Ui.Muted);
    private readonly TextBox email = new() { PlaceholderText = "Saaja e-post" };
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 1000000 };
    private readonly Button pdf, send;
    private string? lastPdf;
    public ArveForm()
    {
        InitializeComponent(); Ui.Setup(this, "Ostukorv ja arve", 1000, 780);
        var top = new Panel { Dock = DockStyle.Top, Height = 154 };
        var summary = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, AutoScroll = true };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 20, 24) };
        Controls.Add(body); Controls.Add(summary); Controls.Add(top);
        Ui.Place(top, Ui.Label("OSTUKORV  /  SINU VALIK", 9, Ui.Muted, true), 28, 18, 600, 26);
        Ui.Place(top, Ui.Label("Kõik hea ühes korvis.", 27, bold: true), 26, 51, 850, 54);
        Ui.Place(top, Ui.Label("Kontrolli koguseid ja salvesta oma valik PDF-arvena.", 11, Ui.Muted), 28, 111, 850, 30);
        Ui.Grid(grid); grid.Dock = DockStyle.Fill; body.Controls.Add(grid);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 12, 0, 0) };
        body.Controls.Add(actions); quantity.Width = 65; actions.Controls.Add(quantity);
        actions.Controls.Add(Ui.Button("Muuda kogust", ChangeQuantity, false));
        actions.Controls.Add(Ui.Button("Eemalda", Remove, false));
        Ui.Place(summary, Ui.Label("Kokkuvõte", 18, bold: true), 22, 23, 260, 40);
        Ui.Place(summary, Ui.Label("TASUMISEKS", 9, Ui.Muted, true), 22, 91, 260, 24);
        Ui.Place(summary, total, 20, 121, 256, 58);
        Ui.Place(summary, Ui.Label("Arve loomine ei kinnita makset\nega vähenda laoseisu.", 10, Ui.Muted), 22, 193, 258, 58);
        pdf = Ui.Button("Salvesta PDF-arve  ↓", Generate);
        Ui.Place(summary, pdf, 22, 276, 256, 44);
        Ui.Place(summary, Ui.Button("Ava viimane arve", (_, _) => { if (lastPdf != null && File.Exists(lastPdf)) Process.Start(new ProcessStartInfo(lastPdf) { UseShellExecute = true }); }, false), 22, 331, 256, 40);
        Ui.Place(summary, Ui.Label("SAADA ARVE E-POSTIGA", 9, Ui.Muted, true), 22, 401, 256, 24);
        Ui.Place(summary, email, 22, 437, 256, 32);
        send = Ui.Button("Saada arve", Send, false); Ui.Place(summary, send, 22, 483, 256, 40);
        Ui.Place(summary, status, 22, 540, 256, 96);
        grid.SelectionChanged += (_, _) => { if (Selected() is { } r) quantity.Value = Math.Clamp(r.Kogus, 1, 1000000); };
        RefreshCart();
    }
    private OstukorviRida? Selected() => grid.CurrentRow?.Cells["Id"].Value is int id ? Ostukorv.Ridad.FirstOrDefault(r => r.Toode.Id == id) : null;
    private void RefreshCart()
    {
        grid.DataSource = Ostukorv.Ridad.Select(r => new { Id = r.Toode.Id, Toode = r.Toode.Toodenimetus, Kogus = r.Kogus, Hind = r.Toode.Hind, Summa = r.Summa }).ToList();
        grid.Columns["Id"].Visible = false; grid.Columns["Toode"].FillWeight = 180;
        grid.Columns["Hind"].DefaultCellStyle.Format = "0.00 €"; grid.Columns["Summa"].DefaultCellStyle.Format = "0.00 €";
        total.Text = $"{Ostukorv.Kokku:F2} €"; pdf.Enabled = Ostukorv.Ridad.Count > 0;
        send.Enabled = lastPdf != null;
        if (Ostukorv.Ridad.Count == 0) status.Text = "Ostukorv on tühi. Vali menüüst Pood ja lisa oma lemmikud.";
    }
    private void Changed() { lastPdf = null; status.Text = "Korv uuendatud. Loo uus arve."; RefreshCart(); }
    private void ChangeQuantity(object? sender, EventArgs e)
    {
        if (Selected() is not { } row) return;
        using var db = new ToodedContext(); var product = db.Toodetabel.Find(row.Toode.Id);
        if (product == null || quantity.Value > product.Kogus) { status.Text = "Soovitud kogust ei ole laos."; return; }
        row.Kogus = (int)quantity.Value; row.Toode = product; Changed();
    }
    private void Remove(object? sender, EventArgs e)
    { if (Selected() is { } row) { Ostukorv.Ridad.Remove(row); Changed(); } }
    private void Generate(object? sender, EventArgs e)
    {
        if (Ostukorv.Ridad.Count == 0) return;
        using var db = new ToodedContext();
        foreach (var row in Ostukorv.Ridad)
        {
            var product = db.Toodetabel.Find(row.Toode.Id);
            if (product == null || row.Kogus > product.Kogus)
            { lastPdf = null; send.Enabled = false; status.Text = $"Kontrolli laoseisu: {row.Toode.Toodenimetus}"; return; }
            row.Toode = product;
        }
        RefreshCart();
        using var dialog = new SaveFileDialog { Filter = L.T("PDF-arve|*.pdf"), InitialDirectory = Kaustad.Arved, FileName = $"Arve_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try { Invoice.Write(dialog.FileName); lastPdf = dialog.FileName; status.Text = "PDF-arve salvestatud."; send.Enabled = true; }
        catch (Exception ex) { lastPdf = null; send.Enabled = false; status.Text = "PDF loomine ebaõnnestus."; MessageBox.Show(ex.Message, L.T("Arve")); }
    }
    private async void Send(object? sender, EventArgs e)
    {
        if (lastPdf == null || !File.Exists(lastPdf)) { status.Text = "Loo kõigepealt PDF-arve."; return; }
        if (!MailAddress.TryCreate(email.Text.Trim(), out var recipient)) { status.Text = "Sisesta korrektne e-post."; return; }
        var from = Environment.GetEnvironmentVariable("TOODED_SMTP_USER");
        var password = Environment.GetEnvironmentVariable("TOODED_SMTP_PASSWORD");
        var host = Environment.GetEnvironmentVariable("TOODED_SMTP_HOST");
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(host))
        { status.Text = "E-post pole seadistatud. SMTP juhend on README failis. PDF-i saad salvestada."; return; }
        send.Enabled = false;
        try
        {
            using var message = new MailMessage(from, recipient.Address, L.T("Tooded · arve"), L.T("Tere! Sinu arve on manuses."));
            message.Attachments.Add(new Attachment(lastPdf));
            using var smtp = new SmtpClient(host, int.TryParse(Environment.GetEnvironmentVariable("TOODED_SMTP_PORT"), out var port) ? port : 587)
            { Credentials = new NetworkCredential(from, password), EnableSsl = true };
            await smtp.SendMailAsync(message);
            if (!IsDisposed) status.Text = "Arve saadetud.";
        }
        catch (Exception ex) { if (!IsDisposed) status.Text = "Saatmine ebaõnnestus: " + ex.Message; }
        finally { if (!IsDisposed) send.Enabled = lastPdf != null; }
    }
}
internal static class Invoice
{
    internal static void Write(string path)
    {
        using var document = new PdfDocument();
        document.Info.Title = L.T("Tooded · arve");
        var title = new XFont("Arial", 28, XFontStyle.Bold);
        var text = new XFont("Arial", 11);
        var bold = new XFont("Arial", 11, XFontStyle.Bold);
        var green = new XSolidBrush(XColor.FromArgb(43, 94, 67));
        XGraphics? g = null; double y = 0;
        void Page()
        {
            g?.Dispose(); var page = document.AddPage(); g = XGraphics.FromPdfPage(page);
            g.DrawString("tooded.", title, green, 42, 62);
            g.DrawString(L.T($"ARVE  /  {DateTime.Now:dd.MM.yyyy}  /  {document.PageCount}"), text, XBrushes.Gray, 340, 56);
            g.DrawString(L.T("Toode"), bold, green, 42, 112);
            g.DrawString(L.T("Kogus"), bold, green, 320, 112);
            g.DrawString(L.T("Hind"), bold, green, 400, 112);
            g.DrawString(L.T("Summa"), bold, green, 490, 112); y = 144;
        }
        try
        {
            Page();
            foreach (var row in Ostukorv.Ridad)
            {
                if (y > 710) Page();
                string name = L.T(row.Toode.Toodenimetus);
                string fullName = name;
                while (g!.MeasureString(name, text).Width > 255 && name.Length > 1) name = name[..^1];
                if (name != fullName) name = name[..Math.Max(1, name.Length - 2)] + "…";
                g.DrawString(name, text, XBrushes.Black, 42, y);
                g.DrawString(row.Kogus.ToString(), text, XBrushes.Black, 320, y);
                g.DrawString($"{row.Toode.Hind:F2} €", text, XBrushes.Black, 400, y);
                g.DrawString($"{row.Summa:F2} €", bold, XBrushes.Black, 490, y);
                y += 30;
            }
            if (y > 680) Page();
            g!.DrawLine(new XPen(XColors.LightGray), 42, y, 550, y);
            g.DrawString(L.T($"Kokku: {Ostukorv.Kokku:F2} €"), new XFont("Arial", 18, XFontStyle.Bold), green, 340, y + 38);
            g.DrawString(L.T("Aitäh! Arve ei ole maksekinnitus."), text, XBrushes.Gray, 42, y + 78);
        }
        finally { g?.Dispose(); }
        document.Save(path);
    }
}


