using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tooded_EFCore;

// Estonian is the source language. UI bindings retain source text, never user input.
internal static class L
{
    private sealed class Binding { public string Source = "", Last = ""; public bool Busy; public string[]? Items; }
    private static readonly ConditionalWeakTable<Control, Binding> bindings = new();
    private static readonly Dictionary<string, string[]> words = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "translations.json")))!;
    public static string Language { get; private set; } = "et";
    private static readonly string[] codes = { "et", "ru", "en" };
    public static void Load()
    {
        try { var code = File.ReadAllText(Path.Combine(Kaustad.Juur, "language.txt")).Trim(); if (codes.Contains(code)) Language = code; }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
        SetCulture();
    }
    private static void SetCulture()
    {
        var culture = CultureInfo.GetCultureInfo(Language == "ru" ? "ru-RU" : Language == "en" ? "en-GB" : "et-EE");
        CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = culture;
    }
    public static string T(string source)
    {
        if (Language == "et" || string.IsNullOrEmpty(source)) return source;
        if (words.TryGetValue(source, out var pair)) return pair[Language == "ru" ? 0 : 1];
        // Dynamic status text uses explicitly marked templates, keeping product/user data intact.
        foreach (var entry in words.Where(w => w.Key.Contains("{0}")))
        {
            var pattern = "^" + Regex.Replace(Regex.Escape(entry.Key), @"\\\{(\d+)\}", m => "(?<p" + m.Groups[1].Value + ">.*?)") + "$";
            var match = Regex.Match(source, pattern, RegexOptions.Singleline);
            if (match.Success)
                return Regex.Replace(entry.Value[Language == "ru" ? 0 : 1], @"\{(\d+)\}", m => T(match.Groups["p" + m.Groups[1].Value].Value));
        }
        if (source.EndsWith(" · Tooded")) return T(source[..^9]) + " · Tooded";
        return source;
    }
    public static string F(string source, params object[] args) => string.Format(T(source), args);
    public static void SetLanguage(string code)
    {
        if (!codes.Contains(code)) return;
        Language = code; SetCulture();
        try { Directory.CreateDirectory(Kaustad.Juur); File.WriteAllText(Path.Combine(Kaustad.Juur, "language.txt"), code); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
        foreach (Form form in Application.OpenForms.Cast<Form>().ToArray()) Apply(form);
    }
    public static ComboBox Selector()
    {
        var selector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Name = "LanguageSelector", AccessibleName = "Language / Язык / Keel" };
        selector.Items.AddRange(new object[] { "Eesti", "Русский", "English" });
        selector.SelectedIndex = Array.IndexOf(codes, Language);
        selector.SelectedIndexChanged += (_, _) => { if (selector.SelectedIndex >= 0 && codes[selector.SelectedIndex] != Language) SetLanguage(codes[selector.SelectedIndex]); };
        return selector;
    }
    public static void Apply(Control control)
    {
        if (control.Name == "LanguageSelector") { ((ComboBox)control).SelectedIndex = Array.IndexOf(codes, Language); return; }
        if (!bindings.TryGetValue(control, out var b))
        {
            b = new Binding { Source = control is TextBox tb ? tb.PlaceholderText : control.Text };
            bindings.Add(control, b);
            control.ControlAdded += (_, e) => Apply(e.Control!);
            if (control is Label or ButtonBase or Form)
                control.TextChanged += (_, _) => Apply(control);
            if (control is ComboBox combo)
            {
                var originalValue = combo.SelectedValue; var originalIndex = combo.SelectedIndex;
                combo.FormattingEnabled = true;
                combo.Format += (_, e) => { if (e.DesiredType == typeof(string)) e.Value = T(e.ListItem is Kategooria category ? category.Kategooria_nimetus : e.Value?.ToString() ?? ""); };
                if (combo.DataSource == null) b.Items = combo.Items.Cast<object>().Select(x => x.ToString() ?? "").ToArray();
                if (combo.DataSource != null && originalValue != null) combo.SelectedValue = originalValue;
                else combo.SelectedIndex = originalIndex;
            }
            if (control is DataGridView grid)
            {
                grid.DataBindingComplete += (_, _) => Apply(grid);
                grid.CellFormatting += (_, e) =>
                {
                    if (e.ColumnIndex >= 0 && grid.Columns[e.ColumnIndex].Name is "Toode" or "Kategooria" && e.Value is string text)
                        e.Value = T(text);
                };
            }
        }
        if (b.Busy) return;
        b.Busy = true;
        try
        {
            if (control is Label or ButtonBase or Form)
            {
                if (control.Text != b.Last && control.Text != b.Source) b.Source = control.Text;
                b.Last = T(b.Source); control.Text = b.Last;
            }
            else if (control is TextBox tb) tb.PlaceholderText = T(b.Source);
            else if (control is ComboBox combo)
            {
                var index = combo.SelectedIndex; var value = combo.SelectedValue;
                if (b.Items != null)
                    for (int i = 0; i < b.Items.Length; i++) combo.Items[i] = T(b.Items[i]);
                // Force Format to be evaluated again without replacing the data source/selection.
                combo.FormattingEnabled = false; combo.FormattingEnabled = true;
                if (combo.DataSource != null && value != null) combo.SelectedValue = value;
                else if (index < combo.Items.Count) combo.SelectedIndex = index;
                combo.Refresh();
            }
            else if (control is DataGridView grid)
            {
                foreach (DataGridViewColumn column in grid.Columns) column.HeaderText = T(column.Name);
                grid.Invalidate();
            }
            foreach (Control child in control.Controls.Cast<Control>().ToArray()) Apply(child);
        }
        finally { b.Busy = false; }
    }
}
