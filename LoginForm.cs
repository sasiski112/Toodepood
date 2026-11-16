namespace Tooded_EFCore;

public partial class LoginForm : Form
{
    private readonly TextBox txtEmail = new(), txtParool = new();
    private readonly Label error = Ui.Label("", 10, Color.Firebrick);
    public LoginForm()
    {
        InitializeComponent(); Ui.Setup(this, "Tere tulemast", 900, 560);
        MaximizeBox = false; FormBorderStyle = FormBorderStyle.FixedSingle;
        var brand = new Panel { BackColor = Ui.Ink, Dock = DockStyle.Left, Width = 390 };
        Controls.Add(brand);
        Ui.Place(brand, Ui.Label("tooded.", 38, Color.White, true), 40, 40, 310, 75);
        Ui.Place(brand, Ui.Label("Väikesed valikud.\nHea igapäev.", 22, Color.FromArgb(220, 235, 184), true), 40, 165, 315, 140);
        Ui.Place(brand, Ui.Label("Värske valik, lihtne ostukorv\nja kõik vajalik ühes kohas.", 12, Color.FromArgb(192, 208, 194)), 42, 331, 300, 70);
        Ui.Place(brand, Ui.Label("SINU IGAPÄEVANE POOD  /  EST. 2026", 8, Color.FromArgb(162, 185, 166)), 42, 482, 320, 30);
        Ui.Place(this, Ui.Label("Tere tulemast tagasi", 23, bold: true), 440, 65, 420, 50);
        Ui.Place(this, Ui.Label("Logi sisse ja leia oma lemmikud.", 11, Ui.Muted), 442, 120, 380, 30);
        Ui.Place(this, Ui.Label("E-post", bold: true), 442, 182, 365, 25);
        txtEmail.PlaceholderText = "nimi@näide.ee"; Ui.Place(this, txtEmail, 442, 213, 365, 32);
        Ui.Place(this, Ui.Label("Parool", bold: true), 442, 265, 365, 25);
        txtParool.UseSystemPasswordChar = true; Ui.Place(this, txtParool, 442, 296, 365, 32);
        var show = new CheckBox { Text = "Näita parooli", AutoSize = true };
        show.CheckedChanged += (_, _) => txtParool.UseSystemPasswordChar = !show.Checked;
        Ui.Place(this, show, 442, 340, 220, 26);
        var login = Ui.Button("Logi sisse    →", Login);
        Ui.Place(this, login, 442, 387, 365, 46); AcceptButton = login;
        Ui.Place(this, Ui.Button("Loo uus konto", Register, false), 442, 441, 365, 40);
        Ui.Place(this, error, 442, 490, 380, 55);
    }
    private void Register(object? sender, EventArgs e)
    {
        using var form = new RegisterForm();
        if (form.ShowDialog(this) != DialogResult.OK) return;
        txtEmail.Text = form.Email; txtParool.Clear(); txtParool.Focus();
        error.ForeColor = Ui.Green; error.Text = "Konto on loodud. Logi nüüd sisse.";
    }
    private void Login(object? sender, EventArgs e)
    {
        error.ForeColor = Color.Firebrick;
        try
        {
            using var db = new ToodedContext();
            var email = txtEmail.Text.Trim().ToLowerInvariant();
            var user = db.Kasutajad.FirstOrDefault(k => k.Email == email);
            if (user == null || !PasswordHelper.Verify(txtParool.Text, user.ParoolHash))
            { error.Text = "Vale e-post või parool. Proovi uuesti."; txtParool.Clear(); txtParool.Focus(); return; }
            AktiivneKasutaja.Nimi = user.Nimi; AktiivneKasutaja.Roll = user.Roll;
            error.Text = ""; txtParool.Clear(); Hide();
            try { using var main = new FormMain(); main.ShowDialog(); }
            finally
            {
                Ostukorv.Tyhjenda(); Receipt.Last = null;
                AktiivneKasutaja.Nimi = ""; AktiivneKasutaja.Roll = "";
                AktiivneKasutaja.OnDemo = false;
                Show(); txtParool.Focus();
            }
        }
        catch (Exception ex) { error.Text = "Sisselogimine ebaõnnestus: " + ex.Message; }
    }
}

