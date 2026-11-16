using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

namespace Tooded_EFCore;

internal static class Dialoogid
{
    internal static void Modaalne(Form form)
    {
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false; form.MinimizeBox = false; form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.CenterParent;
    }

    // Lühike ja viisakas vabandus otsasoleva toote korral
    internal static void Vabandus(IWin32Window? owner, string toode)
    {
        using var f = new Form();
        Ui.Setup(f, "Vabandame", 400, 190); Modaalne(f);
        Ui.Place(f, Ui.Label("Vabandame!", 16, bold: true), 28, 22, 340, 34);
        Ui.Place(f, Ui.Label($"«{toode}» on hetkel otsas. Vaata palun hiljem uuesti.", 11, Ui.Muted), 28, 62, 344, 50);
        var ok = Ui.Button("Selge", (_, _) => f.Close()); Ui.Place(f, ok, 238, 124, 134, 40);
        f.AcceptButton = ok; f.CancelButton = ok;
        f.ShowDialog(owner);
    }
}

// Toote täielik kirjeldus; klient saab siit ka ostukorvi lisada
internal sealed class ToodeDetailForm : Form
{
    internal int Lisatud { get; private set; }
    private readonly Toode product;
    private readonly Label message = Ui.Label("", 10, Ui.Muted);
    private readonly NumericUpDown quantity = new();

    internal ToodeDetailForm(Toode product, bool saabOsta)
    {
        this.product = product;
        Ui.Setup(this, product.Toodenimetus, 700, 460); Dialoogid.Modaalne(this);
        var seis = Ladu.Seis(product.Kogus);
        var art = new ProductArt(product) { Tuhm = seis == LaoSeis.Otsas };
        if (seis == LaoSeis.Otsas) { art.Silt = "OTSAS"; art.SiltTaust = Ladu.OtsasTekst; }
        else if (seis == LaoSeis.Loppemas) { art.Silt = $"LÕPPEMAS · {product.Kogus} tk"; art.SiltTaust = Ladu.LoppemasSilt; }
        Ui.Place(this, art, 0, 0, 280, 460);

        Ui.Place(this, Ui.Label(product.Kategooria?.Kategooria_nimetus.ToUpperInvariant() ?? "", 9, Ui.Muted, true), 306, 24, 370, 22);
        Ui.Place(this, Ui.Label(product.Toodenimetus, 20, bold: true), 304, 48, 372, 74);
        Ui.Place(this, Ui.Label($"{product.Hind:F2} €", 20, bold: true), 304, 126, 150, 36);
        var stockColor = seis == LaoSeis.Otsas ? Ladu.OtsasTekst : seis == LaoSeis.Loppemas ? Ladu.LoppemasTekst : Ui.Muted;
        var stockText = seis == LaoSeis.Otsas ? "Otsas" : seis == LaoSeis.Loppemas ? $"Lõppemas · alles {product.Kogus} tk" : $"Laos: {product.Kogus} tk";
        Ui.Place(this, Ui.Label(stockText, 10, stockColor, seis != LaoSeis.Olemas), 460, 132, 216, 28);

        Ui.Place(this, Ui.Label("KIRJELDUS", 8, Ui.Muted, true), 306, 172, 300, 20);
        var text = new TextBox
        {
            Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Ui.Background,
            ScrollBars = ScrollBars.Vertical, TabStop = false,
            Text = string.IsNullOrWhiteSpace(product.Kirjeldus) ? "Selle toote kirjeldus puudub veel." : product.Kirjeldus
        };
        Ui.Place(this, text, 306, 196, 370, 140);

        var close = Ui.Button("Sulge", (_, _) => Close(), false);
        Ui.Place(this, close, 574, 350, 102, 42); CancelButton = close;
        if (saabOsta)
        {
            quantity.Minimum = 1; quantity.Maximum = Math.Max(1, product.Kogus); quantity.Value = 1; quantity.Enabled = product.Kogus > 0;
            Ui.Place(this, quantity, 306, 356, 70, 30);
            Ui.Place(this, Ui.Button("+  Lisa korvi", (_, _) => Add(), product.Kogus > 0), 386, 350, 178, 42);
        }
        else Ui.Place(this, Ui.Label("Administraator ei saa osta.", 10, Ui.Muted), 306, 356, 250, 30);
        Ui.Place(this, message, 306, 400, 370, 40);
    }

    private void Add()
    {
        if (product.Kogus <= 0) { Dialoogid.Vabandus(this, product.Toodenimetus); return; }
        int q = (int)quantity.Value;
        if (!Ostukorv.Lisa(product, q))
        { message.ForeColor = Color.Firebrick; message.Text = $"Laos on {product.Kogus} tk; korvis juba {Ostukorv.KogusKorvis(product.Id)} tk."; return; }
        Lisatud += q; message.ForeColor = Ui.Green; message.Text = $"Lisatud korvi: {product.Toodenimetus} × {q}.";
    }
}

// Administraatori uue kategooria dialoog
internal sealed class KategooriaDialog : Form
{
    internal Kategooria? Loodud { get; private set; }
    private readonly TextBox name = new() { MaxLength = 50 };
    private readonly TextBox description = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, MaxLength = 300 };
    private readonly Label error = Ui.Label("", 10, Color.Firebrick);

    internal KategooriaDialog()
    {
        Ui.Setup(this, "Uus kategooria", 440, 360); Dialoogid.Modaalne(this);
        Ui.Place(this, Ui.Label("Uus kategooria", 18, bold: true), 28, 20, 380, 38);
        Ui.Place(this, Ui.Label("Nimetus", 10, Ui.Muted), 28, 72, 380, 22);
        Ui.Place(this, name, 28, 96, 384, 30);
        Ui.Place(this, Ui.Label("Kirjeldus (valikuline)", 10, Ui.Muted), 28, 140, 380, 22);
        Ui.Place(this, description, 28, 164, 384, 70);
        Ui.Place(this, error, 28, 242, 384, 36);
        var cancel = Ui.Button("Tühista", (_, _) => Close(), false); Ui.Place(this, cancel, 28, 296, 120, 42);
        var save = Ui.Button("Lisa kategooria", Save); Ui.Place(this, save, 202, 296, 210, 42);
        AcceptButton = save; CancelButton = cancel;
    }

    private void Save(object? sender, EventArgs e)
    {
        if (!AktiivneKasutaja.OnAdmin) return;
        var nimi = name.Text.Trim();
        if (nimi.Length == 0) { error.Text = "Sisesta kategooria nimetus."; return; }
        try
        {
            using var db = new ToodedContext();
            if (db.Kategooriatabel.AsEnumerable().Any(k => string.Equals(k.Kategooria_nimetus, nimi, StringComparison.CurrentCultureIgnoreCase)))
            { error.Text = "Selline kategooria on juba olemas."; return; }
            var uus = new Kategooria { Kategooria_nimetus = nimi, Kirjeldus = string.IsNullOrWhiteSpace(description.Text) ? null : description.Text.Trim() };
            db.Kategooriatabel.Add(uus); db.SaveChanges();
            Loodud = uus; DialogResult = DialogResult.OK;
        }
        catch (Exception ex) { error.Text = "Salvestamine ebaõnnestus: " + ex.Message; }
    }
}

// Uue kliendikonto registreerimine. Konto salvestatakse andmebaasi, seega jääb see alles ka pärast taaskäivitust.
internal sealed class RegisterForm : Form
{
    internal string? Email { get; private set; }
    private readonly TextBox nimi = new() { MaxLength = 60 };
    private readonly TextBox email = new() { MaxLength = 200 };
    private readonly TextBox parool = new() { UseSystemPasswordChar = true, MaxLength = 72 };
    private readonly TextBox parool2 = new() { UseSystemPasswordChar = true, MaxLength = 72 };
    private readonly Label error = Ui.Label("", 10, Color.Firebrick);

    internal RegisterForm()
    {
        Ui.Setup(this, "Loo konto", 460, 560); Dialoogid.Modaalne(this);
        Ui.Place(this, Ui.Label("Loo uus konto", 18, bold: true), 28, 20, 400, 38);
        Ui.Place(this, Ui.Label("Konto jääb alles ka pärast rakenduse taaskäivitamist.", 10, Ui.Muted), 28, 60, 404, 24);
        Field("Nimi", nimi, 100); Field("E-post", email, 168);
        Field("Parool (vähemalt 8 märki)", parool, 236); Field("Korda parooli", parool2, 304);
        var show = new CheckBox { Text = "Näita parooli", AutoSize = true };
        show.CheckedChanged += (_, _) => parool.UseSystemPasswordChar = parool2.UseSystemPasswordChar = !show.Checked;
        Ui.Place(this, show, 28, 376, 220, 26);
        Ui.Place(this, error, 28, 408, 404, 44);
        var cancel = Ui.Button("Tühista", (_, _) => Close(), false); Ui.Place(this, cancel, 28, 466, 120, 44);
        var create = Ui.Button("Loo konto", Create); Ui.Place(this, create, 162, 466, 270, 44);
        AcceptButton = create; CancelButton = cancel;
    }

    private void Field(string label, TextBox box, int y)
    { Ui.Place(this, Ui.Label(label, 10, Ui.Muted), 28, y, 404, 22); Ui.Place(this, box, 28, y + 24, 404, 30); }

    private void Create(object? sender, EventArgs e)
    {
        var n = nimi.Text.Trim(); var mail = email.Text.Trim().ToLowerInvariant();
        if (n.Length < 2) { error.Text = "Nimi peab olema vähemalt 2 märki."; return; }
        if (!MailAddress.TryCreate(mail, out var addr) || !string.Equals(addr.Address, mail, StringComparison.OrdinalIgnoreCase) || !addr.Host.Contains('.'))
        { error.Text = "Sisesta korrektne e-posti aadress."; return; }
        if (parool.Text.Length < 8) { error.Text = "Parool peab olema vähemalt 8 märki."; return; }
        if (parool.Text != parool2.Text) { error.Text = "Paroolid ei kattu."; return; }
        try
        {
            using var db = new ToodedContext();
            if (db.Kasutajad.Any(k => k.Email == mail)) { error.Text = "Selle e-postiga konto on juba olemas."; return; }
            db.Kasutajad.Add(new Kasutaja { Nimi = n, Email = mail, ParoolHash = PasswordHelper.Hash(parool.Text), Roll = "kasutaja" });
            db.SaveChanges();
            Email = mail; DialogResult = DialogResult.OK;
        }
        catch (DbUpdateException) { error.Text = "Selle e-postiga konto on juba olemas."; }
        catch (Exception ex) { error.Text = "Konto loomine ebaõnnestus: " + ex.Message; }
    }
}
