namespace Tooded_EFCore;

public partial class FormMain : Form
{
    private readonly Panel content = new() { Dock = DockStyle.Fill };
    private readonly List<Button> navigation = new();
    private Form? current;
    public FormMain()
    {
        InitializeComponent();
        Ui.Setup(this, "Sinu igapäevane pood", 1240, 820);
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Ui.Ink };
        Controls.Add(content); Controls.Add(sidebar);
        Ui.Place(sidebar, Ui.Label("tooded.", 29, Color.White, true), 24, 24, 180, 64);
        Ui.Place(sidebar, Ui.Label("HEA PÄEV ALGAB SIIT", 8, Color.FromArgb(168, 190, 172)), 27, 88, 180, 24);
        AddNav(sidebar, "01    Pood", 161, () => new Form3());
        AddNav(sidebar, "02    Ostukorv ja arve", 215, () => new ArveForm());
        if (AktiivneKasutaja.OnAdmin) AddNav(sidebar, "03    Tootehaldus", 269, () => new Form2());
        var user = Ui.Label($"{AktiivneKasutaja.Nimi}\n{(AktiivneKasutaja.OnAdmin ? "Administraator" : "Klient")}", 10, Color.White);
        Ui.Place(sidebar, user, 27, ClientSize.Height - 136, 180, 56);
        user.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        var logout = Ui.Button("Logi välja  →", (_, _) => Close(), false);
        Ui.Place(sidebar, logout, 24, ClientSize.Height - 66, 172, 38);
        logout.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        Ui.Place(sidebar, L.Selector(), 24, 340, 172, 30);
        Open(new Form3(), navigation[0]);
    }
    private void AddNav(Panel panel, string text, int y, Func<Form> create)
    {
        var button = Ui.Button(text, (_, _) => { }, false);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(8, 0, 0, 0); button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => Open(create(), button);
        navigation.Add(button); Ui.Place(panel, button, 16, y, 188, 46);
    }
    private void Open(Form form, Button selected)
    {
        current?.Close(); current?.Dispose();
        foreach (var button in navigation)
        { button.BackColor = button == selected ? Color.FromArgb(214, 231, 180) : Ui.Ink; button.ForeColor = button == selected ? Ui.Ink : Color.FromArgb(203, 215, 204); }
        current = form; form.TopLevel = false; form.FormBorderStyle = FormBorderStyle.None;
        form.MinimumSize = Size.Empty; form.Dock = DockStyle.Fill;
        content.Controls.Add(form); form.Show();
    }
    protected override void OnFormClosed(FormClosedEventArgs e)
    { current?.Close(); current?.Dispose(); Ostukorv.Tyhjenda(); base.OnFormClosed(e); }
}
