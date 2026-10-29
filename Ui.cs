using System.Drawing.Drawing2D;

namespace Tooded_EFCore;

internal static class Ui
{
    internal static readonly Color Background = Color.FromArgb(246, 247, 242);
    internal static readonly Color Ink = Color.FromArgb(30, 49, 41);
    internal static readonly Color Muted = Color.FromArgb(112, 124, 115);
    internal static readonly Color Green = Color.FromArgb(43, 94, 67);
    internal static readonly Color Line = Color.FromArgb(224, 229, 219);
    internal static void Setup(Form form, string title, int width, int height)
    {
        form.Text = title + " · Tooded";
        form.Shown += (_, _) => L.Apply(form);
        form.Font = new Font("Segoe UI", 10);
        form.BackColor = Background;
        form.ForeColor = Ink;
        form.ClientSize = new Size(width, height);
        form.StartPosition = FormStartPosition.CenterScreen;
        form.MinimumSize = new Size(width, height);
    }
    internal static Label Label(string text, int size = 10, Color? color = null, bool bold = false) => new()
    {
        Text = text, UseMnemonic = false, ForeColor = color ?? Ink, AutoSize = false,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        TextAlign = ContentAlignment.MiddleLeft
    };
    internal static Button Button(string text, EventHandler action, bool primary = true)
    {
        var button = new Button { Text = text, UseMnemonic = false, Width = 150, Height = 42, Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Green : Color.White,
            ForeColor = primary ? Color.White : Ink, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Line;
        button.Click += action;
        return button;
    }
    internal static void Place(Control parent, Control child, int x, int y, int w, int h)
    { child.SetBounds(x, y, w, h); parent.Controls.Add(child); }
    internal static void Grid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White; grid.BorderStyle = BorderStyle.None;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false; grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 44; grid.RowTemplate.Height = 46;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Background, ForeColor = Muted, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Ink, SelectionBackColor = Color.FromArgb(228, 237, 218), SelectionForeColor = Ink, Padding = new Padding(8) };
        grid.GridColor = Line; grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; grid.MultiSelect = false;
        grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
    }
}

// Locally drawn artwork keeps the catalogue useful offline and avoids remote image dependencies.
internal sealed class ProductArt : Control
{
    private readonly Toode product;
    private readonly Image? photo;
    internal ProductArt(Toode product)
    {
        this.product = product; photo = Kaustad.LaePilt(product.Pilt) ?? ProductPhotos.Load(product);
        DoubleBuffered = true; BackColor = Color.FromArgb(238, 241, 229);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        if (photo != null)
        {
            float scale = Math.Min((float)Width / photo.Width, (float)Height / photo.Height);
            var w = photo.Width * scale; var h = photo.Height * scale;
            g.DrawImage(photo, (Width - w) / 2, (Height - h) / 2, w, h); return;
        }
        g.TranslateTransform(Width / 2f, Height / 2f);
        using var shadow = new SolidBrush(Color.FromArgb(218, 224, 204));
        g.FillEllipse(shadow, -54, 43, 108, 15);
        if (product.KategooriaId == 1)
        {
            using var bottle = new SolidBrush(product.Toodenimetus.Contains("vesi") ? Color.FromArgb(143, 183, 184) : Color.FromArgb(232, 166, 66));
            g.FillRectangle(bottle, -27, -35, 54, 87); g.FillRectangle(bottle, -14, -57, 28, 25);
            using var cap = new SolidBrush(Ui.Green); g.FillRectangle(cap, -16, -64, 32, 10);
            g.FillRectangle(Brushes.White, -27, -3, 54, 31);
            using var f = new Font("Segoe UI", 9, FontStyle.Bold);
            g.DrawString("TOO", f, cap, -16, 3);
        }
        else if (product.KategooriaId == 3)
        {
            using var wrap = new SolidBrush(Color.FromArgb(116, 79, 63));
            g.RotateTransform(-12); g.FillRectangle(wrap, -42, -56, 84, 112);
            using var paper = new SolidBrush(Color.FromArgb(243, 217, 173));
            g.FillRectangle(paper, -42, -14, 84, 55);
            using var f = new Font("Segoe UI", 11, FontStyle.Bold); g.DrawString("MAGUS", f, wrap, -33, 3);
        }
        else
        {
            var name = product.Toodenimetus.ToLowerInvariant();
            using var fruit = new SolidBrush(name.Contains("apels") ? Color.FromArgb(239, 155, 48) : name.Contains("tomat") ? Color.FromArgb(215, 88, 62) : Color.FromArgb(130, 164, 72));
            g.FillEllipse(fruit, -49, -39, 98, 91);
            using var leaf = new SolidBrush(Ui.Green); g.FillEllipse(leaf, 0, -53, 34, 16);
            using var shine = new Pen(Color.FromArgb(100, Color.White), 5); g.DrawArc(shine, -35, -27, 65, 62, 195, 55);
        }
    }
    protected override void Dispose(bool disposing) { if (disposing) photo?.Dispose(); base.Dispose(disposing); }
}

