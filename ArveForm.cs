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
    private readonly Button pdf, send, confirm;
    private bool sending;
    public ArveForm()
    {
        InitializeComponent(); Ui.Setup(this, "Ostukorv ja arve", 1000, 780);
        var top = new Panel { Dock = DockStyle.Top, Height = 154 };
        var summary = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, AutoScroll = true };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 0, 20, 24) };
        Controls.Add(body); Controls.Add(summary); Controls.Add(top);
        Ui.Place(top, Ui.Label("OSTUKORV  /  SINU VALIK", 9, Ui.Muted, true), 28, 18, 600, 26);
        Ui.Place(top, Ui.Label("Kõik hea ühes korvis.", 27, bold: true), 26, 51, 850, 54);
        Ui.Place(top, Ui.Label("Kontrolli ostukorvi ja kinnita ost. Seejärel saad valida tšeki.", 11, Ui.Muted), 28, 111, 850, 30);
        Ui.Grid(grid); grid.Dock = DockStyle.Fill; body.Controls.Add(grid);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 12, 0, 0) };
        body.Controls.Add(actions); quantity.Width = 65; actions.Controls.Add(quantity);
        actions.Controls.Add(Ui.Button("Muuda kogust", ChangeQuantity, false));
        actions.Controls.Add(Ui.Button("Eemalda", Remove, false));
        Ui.Place(summary, Ui.Label("Kokkuvõte", 18, bold: true), 22, 23, 260, 40);
        Ui.Place(summary, Ui.Label("TASUMISEKS", 9, Ui.Muted, true), 22, 91, 260, 24);
        Ui.Place(summary, total, 20, 121, 256, 58);
        Ui.Place(summary, Ui.Label(AktiivneKasutaja.OnDemo ? "Demopood: proovi ostmist.\nLaoseisu ja müüke ei muudeta." : "Pärast ostu küsime, kas soovid tšekki.", 10, Ui.Muted), 22, 193, 258, 58);
        confirm = Ui.Button(AktiivneKasutaja.OnDemo ? "Proovi ostu  ✓" : "Kinnita ost  ✓", Confirm);
        Ui.Place(summary, confirm, 22, 262, 256, 44);
        pdf = Ui.Button("Salvesta viimane tšekk  ↓", Generate, false);
        Ui.Place(summary, pdf, 22, 318, 256, 44);
        Ui.Place(summary, Ui.Button("Ava viimane tšekk", (_, _) => OpenReceipt(), false), 22, 372, 256, 40);
        Ui.Place(summary, Ui.Label("VIIMASE OSTU TŠEKK", 9, Ui.Muted, true), 22, 440, 256, 24);
        Ui.Place(summary, email, 22, 466, 256, 32);
        send = Ui.Button("Saada tšekk", Send, false); Ui.Place(summary, send, 22, 512, 256, 40);
        Ui.Place(summary, status, 22, 562, 256, 96);
        grid.SelectionChanged += (_, _) => { if (Selected() is { } r) quantity.Value = Math.Clamp(r.Kogus, 1, 1000000); };
        RefreshCart();
    }
    private OstukorviRida? Selected() => grid.CurrentRow?.Cells["Id"].Value is int id ? Ostukorv.Ridad.FirstOrDefault(r => r.Toode.Id == id) : null;
    private void RefreshCart()
    {
        grid.DataSource = Ostukorv.Ridad.Select(r => new { Id = r.Toode.Id, Toode = r.Toode.Toodenimetus, Kogus = r.Kogus, Hind = r.Toode.Hind, Summa = r.Summa }).ToList();
        grid.Columns["Id"].Visible = false; grid.Columns["Toode"].FillWeight = 180;
        grid.Columns["Hind"].DefaultCellStyle.Format = "0.00 €"; grid.Columns["Summa"].DefaultCellStyle.Format = "0.00 €";
        total.Text = $"{Ostukorv.Kokku:F2} €"; pdf.Enabled = Receipt.Last != null; confirm.Enabled = Ostukorv.Ridad.Count > 0 && !AktiivneKasutaja.OnAdmin;
        send.Enabled = Receipt.Last != null && !sending;
        if (Ostukorv.Ridad.Count == 0) status.Text = "Ostukorv on tühi. Vali menüüst Pood ja lisa oma lemmikud.";
    }
    // Ostu kinnitamine: vähendab laoseisu ja salvestab müügiread, millest arvutatakse ostusoovitused
    private void Confirm(object? sender, EventArgs e)
    {
        if (AktiivneKasutaja.OnAdmin || Ostukorv.Ridad.Count == 0) return;
        bool demo = AktiivneKasutaja.OnDemo;
        if (MessageBox.Show(this, $"{(demo ? "Proovi ostu" : "Kinnita ost")} summas {Ostukorv.Kokku:F2} €?", "Tooded", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var receipt = new Receipt(Ostukorv.Ridad, demo);
        try
        {
            using var db = new ToodedContext();
            if (!demo) Ostuteenus.Kinnita(db, Ostukorv.Ridad);
            else foreach (var row in Ostukorv.Ridad)
            {
                var product = db.Toodetabel.Find(row.Toode.Id);
                if (product == null || row.Kogus <= 0 || product.Kogus < row.Kogus || product.Hind != row.Toode.Hind)
                    throw new InvalidOperationException("Laoseis või hind on muutunud. Kontrolli ostukorvi.");
            }
        }
        catch (Exception ex) { status.Text = "Ost jäi kinnitamata."; MessageBox.Show(this, ex.Message, "Tooded"); return; }
        Receipt.Last = receipt;
        Ostukorv.Tyhjenda(); RefreshCart();
        status.Text = demo ? "Demoost lõpetatud. Aitäh!" : "Ost kinnitatud. Aitäh!";
        if (MessageBox.Show(this, "Kas soovid tšekki?", "Aitäh ostu eest!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            OpenReceipt();
    }
    private void Changed() { RefreshCart(); status.Text = "Ostukorv uuendatud."; }
    private void ChangeQuantity(object? sender, EventArgs e)
    {
        if (Selected() is not { } row) return;
        try
        {
            using var db = new ToodedContext(); var product = db.Toodetabel.Find(row.Toode.Id);
            if (product == null || quantity.Value > product.Kogus) { status.Text = "Soovitud kogust ei ole laos."; return; }
            row.Kogus = (int)quantity.Value; row.Toode = product; Changed();
        }
        catch (Exception ex) { status.Text = "Koguse muutmine ebaõnnestus: " + ex.Message; }
    }
    private void Remove(object? sender, EventArgs e)
    { if (Selected() is { } row) { Ostukorv.Ridad.Remove(row); Changed(); } }
    private string EnsureReceiptPdf()
    {
        var receipt = Receipt.Last ?? throw new InvalidOperationException("Kinnita kõigepealt ost.");
        if (receipt.PdfPath != null && File.Exists(receipt.PdfPath)) return receipt.PdfPath;
        Directory.CreateDirectory(Kaustad.Arved);
        string path = Path.Combine(Kaustad.Arved, "Tsekk_" + receipt.Number + ".pdf");
        Invoice.Write(path, receipt);
        receipt.PdfPath = path;
        return path;
    }
    private void OpenReceipt()
    {
        try
        {
            string path = EnsureReceiptPdf();
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            status.Text = "Tšekk on avatud ja salvestatud. Soovi korral saada see e-postiga.";
        }
        catch (Exception ex) { status.Text = "Tšeki avamine ebaõnnestus. Ost jääb kinnitatuks. " + ex.Message; }
    }
    private void Generate(object? sender, EventArgs e)
    {
        if (Receipt.Last == null) return;
        try
        {
            string source = EnsureReceiptPdf();
            using var dialog = new SaveFileDialog { Filter = "PDF-tšekk|*.pdf", InitialDirectory = Kaustad.Arved, FileName = Path.GetFileName(source) };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(dialog.FileName), StringComparison.OrdinalIgnoreCase))
                File.Copy(source, dialog.FileName, true);
            status.Text = "Tšekk salvestatud.";
        }
        catch (Exception ex) { status.Text = "Tšeki salvestamine ebaõnnestus. Ost jääb kinnitatuks. " + ex.Message; }
    }

    private async void Send(object? sender, EventArgs e)
    {
        if (sending || Receipt.Last == null) return;
        if (!MailAddress.TryCreate(email.Text.Trim(), out var recipient)) { status.Text = "Sisesta korrektne e-post."; return; }
        var from = Environment.GetEnvironmentVariable("TOODED_SMTP_USER");
        var password = Environment.GetEnvironmentVariable("TOODED_SMTP_PASSWORD");
        var host = Environment.GetEnvironmentVariable("TOODED_SMTP_HOST");
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(host))
        { status.Text = "E-post pole seadistatud. SMTP juhend on README failis. PDF-i saad salvestada."; return; }
        sending = true; send.Enabled = false;
        try
        {
            using var message = new MailMessage(from, recipient.Address, L.T("Tooded · tšekk"), L.T("Tere! Sinu ostutšekk on manuses."));
            message.Attachments.Add(new Attachment(EnsureReceiptPdf()));
            using var smtp = new SmtpClient(host, int.TryParse(Environment.GetEnvironmentVariable("TOODED_SMTP_PORT"), out var port) ? port : 587)
            { Credentials = new NetworkCredential(from, password), EnableSsl = true };
            await smtp.SendMailAsync(message);
            if (!IsDisposed) status.Text = "Tšekk saadetud.";
        }
        catch (Exception ex) { if (!IsDisposed) status.Text = "Saatmine ebaõnnestus: " + ex.Message; }
        finally { sending = false; if (!IsDisposed) send.Enabled = Receipt.Last != null; }
    }
}
internal static class Invoice
{
    internal static void Write(string path, Receipt receipt)
    {
        using var document = new PdfDocument();
        document.Info.Title = L.T("Tooded · tšekk");
        var title = new XFont("Arial", 28, XFontStyle.Bold);
        var text = new XFont("Arial", 11);
        var bold = new XFont("Arial", 11, XFontStyle.Bold);
        var green = new XSolidBrush(XColor.FromArgb(43, 94, 67));
        XGraphics? g = null; double y = 0;
        void Page()
        {
            g?.Dispose(); var page = document.AddPage(); g = XGraphics.FromPdfPage(page);
            g.DrawString("tooded.", title, green, 42, 62);
            g.DrawString(receipt.Demo ? "DEMO · OSTUTŠEKK" : "OSTUTŠEKK", bold, green, 340, 42);
            g.DrawString(receipt.Number, text, XBrushes.Gray, 340, 60);
            g.DrawString($"{receipt.Created:dd.MM.yyyy HH:mm:ss} · lk {document.PageCount}", text, XBrushes.Gray, 340, 78);
            g.DrawString(L.T("Toode"), bold, green, 42, 112);
            g.DrawString(L.T("Kogus"), bold, green, 320, 112);
            g.DrawString(L.T("Hind"), bold, green, 400, 112);
            g.DrawString(L.T("Summa"), bold, green, 490, 112); y = 144;
        }
        try
        {
            Page();
            foreach (var row in receipt.Lines)
            {
                if (y > 710) Page();
                string name = L.T(row.Name);
                string fullName = name;
                while (g!.MeasureString(name, text).Width > 255 && name.Length > 1) name = name[..^1];
                if (name != fullName) name = name[..Math.Max(1, name.Length - 2)] + "…";
                g.DrawString(name, text, XBrushes.Black, 42, y);
                g.DrawString(row.Quantity.ToString(), text, XBrushes.Black, 320, y);
                g.DrawString($"{row.Price:F2} €", text, XBrushes.Black, 400, y);
                g.DrawString($"{row.Total:F2} €", bold, XBrushes.Black, 490, y);
                y += 30;
            }
            if (y > 680) Page();
            g!.DrawLine(new XPen(XColors.LightGray), 42, y, 550, y);
            g.DrawString(L.T($"Kokku: {receipt.Total:F2} €"), new XFont("Arial", 18, XFontStyle.Bold), green, 340, y + 38);
            g.DrawString(receipt.Demo ? "DEMO — laoseisu ega müüke ei muudetud." : "Aitäh ostu eest! Õppepoe ostutšekk.", text, XBrushes.Gray, 42, y + 78);
        }
        finally { g?.Dispose(); }
        document.Save(path);
    }
}


