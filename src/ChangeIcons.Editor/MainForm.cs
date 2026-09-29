using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ChangeIcons.Editor;

public class MainForm : Form
{
    private readonly string _pluginFolder;
    private readonly string _iconsJson;
    private readonly Profiles _profiles;
    private GameIcons _game;
    private IconsFile _file;
    private readonly List<EditableIcon> _icons = [];
    private readonly Dictionary<EditableIcon, Bitmap?> _imageCache = [];
    private readonly string _previewNickname;
    private bool _dirty;
    private bool _loading;

    // Icons tab
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 30, IntegralHeight = false };
    private readonly Button _newButton = new() { Text = "New icon", AutoSize = true };
    private readonly Button _removeButton = new() { Text = "Remove", AutoSize = true };
    private readonly Label _hint = new() { AutoSize = true, ForeColor = Color.DarkGoldenrod, Padding = new Padding(0, 0, 0, 6) };
    private readonly Label _slotLabel = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Anchor = AnchorStyles.Left };
    private readonly TextBox _name = new() { Dock = DockStyle.Fill };
    private readonly Button _colorSwatch = Swatch();
    private readonly TextBox _colorHex = new() { Width = 90 };
    private readonly Button _colorReset = new() { Text = "Game's color", AutoSize = true };
    private readonly ComboBox _source = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _import = new() { Text = "Import image...", AutoSize = true };
    private readonly CheckBox _tintCheck = new() { Text = "Recolor to", AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Button _tintSwatch = Swatch();
    private readonly TextBox _tintHex = new() { Width = 90 };
    private readonly PreviewPanel _preview = new() { Dock = DockStyle.Fill };

    // Profile tab
    private readonly ComboBox _profileBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckedListBox _owned = new() { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
    private readonly ComboBox _shown = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _applyProfile = new() { Text = "Save to profile", AutoSize = true };
    private readonly Label _profileNote = new() { AutoSize = true, MaximumSize = new Size(560, 0) };

    private readonly Button _save = new() { Text = "Save icons", AutoSize = true, Enabled = false };
    private readonly Label _status = new() { AutoSize = true, Anchor = AnchorStyles.Left };

    private readonly ColorDialog _colorDialog = new() { FullOpen = true, AnyColor = true };

    private record SourceItem(IconSource Kind, string? Game, string Label)
    {
        public override string ToString() => Label;
    }

    private record CategoryItem(int Value, string Label)
    {
        public override string ToString() => Label;
    }

    public MainForm(string sptPath)
    {
        _pluginFolder = Path.Combine(sptPath, "BepInEx", "plugins", "ChangeIcons");
        _iconsJson = Path.Combine(_pluginFolder, "icons.json");
        _profiles = new Profiles(sptPath, Path.Combine(_pluginFolder, "backups"));
        _game = new GameIcons(_pluginFolder);
        _file = new IconsFile();
        _previewNickname = _profiles.List().FirstOrDefault(p => p.HasCharacter)?.Nickname ?? "Nickname";

        Text = "ChangeIcons Editor";
        Size = new Size(960, 640);
        MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildIconsTab());
        tabs.TabPages.Add(BuildProfileTab());
        tabs.SelectedIndexChanged += (_, _) => { if (tabs.SelectedIndex == 1) LoadProfiles(); };

        var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.Controls.Add(_status, 0, 0);
        bottom.Controls.Add(_save, 1, 0);

        Controls.Add(tabs);
        Controls.Add(bottom);

        _save.Click += (_, _) => Save();
        Activated += (_, _) => RefreshGameIcons();
        FormClosing += OnClosing;

        if (!Directory.Exists(_pluginFolder))
        {
            MessageBox.Show($"The ChangeIcons plugin isn't installed:\n{_pluginFolder}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        LoadIcons();
        SetStatus($"SPT: {sptPath}");
    }

    // ---------------------------------------------------------------- Icons tab

    private TabPage BuildIconsTab()
    {
        var page = new TabPage("Icons") { Padding = new Padding(8) };

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var listButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        listButtons.Controls.AddRange([_newButton, _removeButton]);
        left.Controls.Add(_list, 0, 0);
        left.Controls.Add(listButtons, 0, 1);

        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(8, 0, 0, 0) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        form.Controls.Add(_hint, 0, 0);
        form.SetColumnSpan(_hint, 3);

        form.Controls.Add(FieldLabel("Name"), 0, 1);
        form.Controls.Add(_name, 1, 1);
        form.Controls.Add(_slotLabel, 2, 1);

        form.Controls.Add(FieldLabel("Name color"), 0, 2);
        form.Controls.Add(Row(_colorSwatch, _colorHex), 1, 2);
        form.Controls.Add(_colorReset, 2, 2);

        form.Controls.Add(FieldLabel("Icon"), 0, 3);
        form.Controls.Add(_source, 1, 3);
        form.Controls.Add(_import, 2, 3);

        form.Controls.Add(FieldLabel("Icon color"), 0, 4);
        form.Controls.Add(Row(_tintCheck, _tintSwatch, _tintHex), 1, 4);

        form.Controls.Add(_preview, 0, 5);
        form.SetColumnSpan(_preview, 3);
        for (var i = 0; i < 5; i++)
        {
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(form);
        page.Controls.Add(split);
        Shown += (_, _) => split.SplitterDistance = 250;

        _list.DrawItem += DrawListItem;
        _list.SelectedIndexChanged += (_, _) => ShowSelected();
        _newButton.Click += (_, _) => AddCustom();
        _removeButton.Click += (_, _) => RemoveSelected();

        _name.TextChanged += (_, _) => Edit(i => i.Name = string.IsNullOrWhiteSpace(_name.Text) ? null : _name.Text.Trim());
        _colorSwatch.Click += (_, _) => PickColor(_colorHex);
        _colorHex.TextChanged += (_, _) => Edit(i =>
        {
            if (Images.TryParseColor(_colorHex.Text, out var c))
            {
                i.Color = Images.ToHex(c);
            }
        });
        _colorReset.Click += (_, _) =>
        {
            Edit(i => i.Color = null);
            ShowSelected();
        };
        _source.SelectedIndexChanged += (_, _) => Edit(i =>
        {
            if (_source.SelectedItem is not SourceItem s)
            {
                return;
            }

            // No image yet: ask for one; the source only changes once there is one
            if (s.Kind == IconSource.File && i.File == null)
            {
                BeginInvoke(ImportImage);
                return;
            }

            i.Source = s.Kind;
            i.GameIcon = s.Kind == IconSource.Game ? s.Game : i.GameIcon;
        });
        _import.Click += (_, _) => ImportImage();
        _tintCheck.CheckedChanged += (_, _) =>
        {
            Edit(i => i.Tint = _tintCheck.Checked ? (Images.TryParseColor(_tintHex.Text, out var c) ? Images.ToHex(c) : i.Color ?? "#FFFFFF") : null);
            ShowSelected();
        };
        _tintSwatch.Click += (_, _) => PickColor(_tintHex);
        _tintHex.TextChanged += (_, _) => Edit(i =>
        {
            if (_tintCheck.Checked && Images.TryParseColor(_tintHex.Text, out var c))
            {
                i.Tint = Images.ToHex(c);
            }
        });

        return page;
    }

    private void LoadIcons()
    {
        _icons.Clear();
        try
        {
            _file = IconsFile.Load(_iconsJson);
            foreach (var entry in _file.Icons)
            {
                _icons.Add(IconsFile.ToEditable(entry));
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Couldn't read icons.json, starting fresh.\n\n{e.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _file = new IconsFile();
        }

        // Every game icon is listed, changed or not
        foreach (var name in Categories.WithIcons)
        {
            var value = Categories.Parse(name)!.Value;
            if (_icons.All(i => i.Value != value))
            {
                _icons.Add(new EditableIcon { Value = value });
            }
        }

        _icons.Sort((a, b) => a.IsCustom != b.IsCustom ? (a.IsCustom ? -1 : 1) : a.Value.CompareTo(b.Value));
        RefreshList(_icons.FirstOrDefault());
        UpdateHint();
    }

    private EditableIcon? Selected => _list.SelectedItem as EditableIcon;

    private void RefreshList(EditableIcon? select)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var icon in _icons)
        {
            _list.Items.Add(icon);
        }

        _list.EndUpdate();
        _list.SelectedItem = select;
        if (_list.SelectedIndex < 0 && _list.Items.Count > 0)
        {
            _list.SelectedIndex = 0;
        }
    }

    private void ShowSelected()
    {
        var icon = Selected;
        _loading = true;
        try
        {
            foreach (Control c in new Control[] { _name, _colorSwatch, _colorHex, _colorReset, _source, _import, _tintCheck, _tintSwatch, _tintHex, _removeButton })
            {
                c.Enabled = icon != null;
            }

            if (icon == null)
            {
                return;
            }

            _removeButton.Text = icon.IsCustom ? "Remove" : "Reset";
            _slotLabel.Text = icon.IsCustom ? $"new icon, flag {icon.Value}" : $"game icon: {Categories.EnumName(icon.Value)}";
            _name.PlaceholderText = icon.IsCustom ? "Required for a new icon" : $"Game's name ({Categories.Label(icon.Value)})";
            _name.Text = icon.Name ?? "";

            var color = NameColor(icon);
            _colorHex.Text = Images.ToHex(color);
            _colorSwatch.BackColor = Color.FromArgb(255, color);
            _colorReset.Enabled = !icon.IsCustom && icon.Color != null;

            _source.Items.Clear();
            if (!icon.IsCustom)
            {
                _source.Items.Add(new SourceItem(IconSource.Own, null, "Its own game icon"));
            }

            foreach (var game in Categories.WithIcons.Where(g => icon.IsCustom || g != Categories.EnumName(icon.Value)))
            {
                _source.Items.Add(new SourceItem(IconSource.Game, game, $"Game icon: {Categories.Label(game)}"));
            }

            _source.Items.Add(new SourceItem(IconSource.File, null, icon.File == null ? "Image file..." : $"Image file: {Path.GetFileName(icon.File)}"));
            _source.SelectedItem = _source.Items.Cast<SourceItem>().FirstOrDefault(s =>
                s.Kind == icon.Source && (s.Kind != IconSource.Game || string.Equals(s.Game, icon.GameIcon, StringComparison.OrdinalIgnoreCase)));

            _tintCheck.Checked = icon.Tint != null;
            _tintSwatch.Enabled = _tintHex.Enabled = icon.Tint != null;
            if (Images.TryParseColor(icon.Tint, out var tint))
            {
                _tintHex.Text = Images.ToHex(tint);
                _tintSwatch.BackColor = tint;
            }
            else
            {
                _tintHex.Text = "";
                _tintSwatch.BackColor = SystemColors.Control;
            }
        }
        finally
        {
            _loading = false;
        }

        UpdatePreview();
    }

    private void Edit(Action<EditableIcon> change)
    {
        if (_loading || Selected is not { } icon)
        {
            return;
        }

        change(icon);
        _imageCache.Remove(icon);
        if (Images.TryParseColor(_colorHex.Text, out var c))
        {
            _colorSwatch.BackColor = Color.FromArgb(255, c);
        }

        if (Images.TryParseColor(_tintHex.Text, out var t))
        {
            _tintSwatch.BackColor = t;
        }

        _colorReset.Enabled = !icon.IsCustom && icon.Color != null;
        MarkDirty();
        _list.Invalidate();
        UpdatePreview();
    }

    private void AddCustom()
    {
        int value;
        try
        {
            value = Categories.NextFreeCustom(_icons.Select(i => i.Value));
        }
        catch (InvalidOperationException e)
        {
            MessageBox.Show(e.Message, Text);
            return;
        }

        var icon = new EditableIcon { Value = value, Name = "New icon", Color = "#FFFFFF", Source = IconSource.Game, GameIcon = "Default" };
        _icons.Insert(_icons.Count(i => i.IsCustom), icon);
        RefreshList(icon);
        MarkDirty();
        _name.Focus();
        _name.SelectAll();
    }

    private void RemoveSelected()
    {
        if (Selected is not { } icon)
        {
            return;
        }

        if (icon.IsCustom)
        {
            if (MessageBox.Show($"Remove \"{icon.Name}\"? A profile that has flag {icon.Value} goes back to the plain icon.", Text, MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                return;
            }

            _icons.Remove(icon);
            RefreshList(_icons.FirstOrDefault());
        }
        else
        {
            icon.Name = icon.Color = icon.GameIcon = icon.File = icon.Tint = null;
            icon.Source = IconSource.Own;
            _imageCache.Remove(icon);
            ShowSelected();
            _list.Invalidate();
        }

        MarkDirty();
    }

    private void PickColor(TextBox target)
    {
        if (Images.TryParseColor(target.Text, out var current))
        {
            _colorDialog.Color = current;
        }

        if (_colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            if (target == _tintHex && !_tintCheck.Checked)
            {
                _tintCheck.Checked = true;
            }

            target.Text = Images.ToHex(Color.FromArgb(255, _colorDialog.Color));
        }
    }

    private void ImportImage()
    {
        if (Selected is not { } icon)
        {
            return;
        }

        using var dialog = new OpenFileDialog { Title = "Pick an icon image", Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            if (icon.File == null)
            {
                ShowSelected();
            }

            return;
        }

        using var image = Images.TryLoad(dialog.FileName);
        if (image == null)
        {
            MessageBox.Show("That file isn't an image this can read.", Text);
            return;
        }

        // Into the plugin's icons folder, square, at most 128 px (or the game's size if bigger)
        var folder = Path.Combine(_pluginFolder, "icons");
        Directory.CreateDirectory(folder);
        var baseName = new string(Path.GetFileNameWithoutExtension(dialog.FileName).Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray());
        var fileName = baseName + ".png";
        for (var n = 2; File.Exists(Path.Combine(folder, fileName)); n++)
        {
            fileName = $"{baseName}-{n}.png";
        }

        using var square = Images.FitSquare(image, Math.Max(128, _game.IconSize));
        square.Save(Path.Combine(folder, fileName), ImageFormat.Png);

        icon.Source = IconSource.File;
        icon.File = "icons/" + fileName;
        _imageCache.Remove(icon);
        MarkDirty();
        ShowSelected();
        _list.Invalidate();
    }

    private Color NameColor(EditableIcon icon)
    {
        if (Images.TryParseColor(icon.Color, out var own))
        {
            return own;
        }

        return Images.TryParseColor(_game.Get(Categories.EnumName(icon.Value))?.Color, out var game) ? game : Color.White;
    }

    private string DisplayName(EditableIcon icon) => icon.Name ?? Categories.Label(icon.Value);

    /// <summary>The picture the game will show for this icon, recolor included.</summary>
    private Bitmap? IconImage(EditableIcon icon)
    {
        if (_imageCache.TryGetValue(icon, out var cached))
        {
            return cached;
        }

        var source = icon.Source switch
        {
            IconSource.Own => _game.Load(Categories.EnumName(icon.Value)),
            IconSource.Game => _game.Load(icon.GameIcon ?? "Default"),
            _ => icon.File == null ? null : Images.TryLoad(Path.Combine(_pluginFolder, icon.File)),
        };

        if (source != null && Images.TryParseColor(icon.Tint, out var tint))
        {
            var tinted = Images.Tint(source, tint);
            source.Dispose();
            source = tinted;
        }

        _imageCache[icon] = source;
        return source;
    }

    private void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0 || _list.Items[e.Index] is not EditableIcon icon)
        {
            return;
        }

        var g = e.Graphics;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        var box = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 3, e.Bounds.Height - 6, e.Bounds.Height - 6);
        using (var dark = new SolidBrush(Color.FromArgb(24, 26, 27)))
        {
            g.FillRectangle(dark, box);
        }

        if (IconImage(icon) is { } image)
        {
            g.DrawImage(image, box);
        }

        var selected = (e.State & DrawItemState.Selected) != 0;
        var textRect = new Rectangle(box.Right + 6, e.Bounds.Y, e.Bounds.Width - box.Right - 8, e.Bounds.Height);
        var sub = icon.IsCustom ? $"new, {icon.Value}" : icon.IsChanged ? "changed" : "game";
        TextRenderer.DrawText(g, DisplayName(icon), Font, textRect, selected ? SystemColors.HighlightText : SystemColors.ControlText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, sub, Font, textRect, selected ? SystemColors.HighlightText : SystemColors.GrayText, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
    }

    private void UpdatePreview()
    {
        if (Selected is not { } icon)
        {
            return;
        }

        // The settings dropdown draws built-in icons from a fixed sprite sheet the plugin can't
        // change, and custom ones without a picture
        var dropdownImage = icon.IsCustom ? null : _game.Load(Categories.EnumName(icon.Value));
        _preview.SetPreview(IconImage(icon), dropdownImage, _previewNickname, DisplayName(icon), NameColor(icon), icon.IsCustom);
    }

    private void RefreshGameIcons()
    {
        var hadIcons = _game.Available;
        var fresh = new GameIcons(_pluginFolder);
        if (fresh.Available && !hadIcons)
        {
            _game = fresh;
            _imageCache.Clear();
            _list.Invalidate();
            UpdateHint();
            UpdatePreview();
        }
    }

    private void UpdateHint()
    {
        _hint.Text = _game.Available
            ? ""
            : "The game's own icons appear here after you start the game once with the plugin installed.";
        _hint.Visible = !_game.Available;
    }

    // ---------------------------------------------------------------- Saving

    private void Save()
    {
        var unnamed = _icons.FirstOrDefault(i => i.IsCustom && string.IsNullOrWhiteSpace(i.Name));
        if (unnamed != null)
        {
            _list.SelectedItem = unnamed;
            MessageBox.Show("A new icon needs a name.", Text);
            return;
        }

        var file = new IconsFile { DumpOriginalIcons = _file.DumpOriginalIcons };
        try
        {
            foreach (var icon in _icons.Where(i => i.IsChanged))
            {
                var entry = new IconsFile.Entry
                {
                    Category = icon.Key,
                    Name = icon.Name,
                    Color = icon.Color,
                    Editor = new IconsFile.EditorInfo { Source = icon.Source.ToString(), GameIcon = icon.GameIcon, File = icon.File, Tint = icon.Tint },
                };

                if (icon.Tint != null)
                {
                    // The plugin takes a finished PNG, so a recolor is saved as one
                    var image = IconImage(icon) ?? throw new InvalidOperationException(
                        $"\"{DisplayName(icon)}\" is recolored, but the game's icons aren't saved yet. Start the game once with the plugin, then save again.");
                    var relative = $"icons/generated/{icon.Key}.png";
                    Directory.CreateDirectory(Path.Combine(_pluginFolder, "icons", "generated"));
                    image.Save(Path.Combine(_pluginFolder, relative), ImageFormat.Png);
                    entry.Icon = relative;
                }
                else if (icon.Source == IconSource.Game)
                {
                    entry.IconFrom = icon.GameIcon;
                }
                else if (icon.Source == IconSource.File)
                {
                    entry.Icon = icon.File;
                }

                file.Icons.Add(entry);
            }

            Directory.CreateDirectory(_pluginFolder);
            file.Save(_iconsJson);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show($"Not saved.\n\n{e.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _file = file;
        _dirty = false;
        _save.Enabled = false;
        SetStatus($"Saved {DateTime.Now:HH:mm:ss}. If the game is running, it picks this up: reopen a screen to see it.");
    }

    private void MarkDirty()
    {
        _dirty = true;
        _save.Enabled = true;
        SetStatus("Unsaved changes");
    }

    private void SetStatus(string text) => _status.Text = text;

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_dirty)
        {
            return;
        }

        var answer = MessageBox.Show("Save your icon changes?", Text, MessageBoxButtons.YesNoCancel);
        if (answer == DialogResult.Cancel)
        {
            e.Cancel = true;
        }
        else if (answer == DialogResult.Yes)
        {
            Save();
            e.Cancel = _dirty;
        }
    }

    // ---------------------------------------------------------------- Profile tab

    private TabPage BuildProfileTab()
    {
        var page = new TabPage("Profile") { Padding = new Padding(8) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(FieldLabel("Profile"), 0, 0);
        layout.Controls.Add(_profileBox, 1, 0);
        layout.Controls.Add(FieldLabel("Icons you have"), 0, 1);
        layout.Controls.Add(_owned, 1, 1);
        layout.Controls.Add(FieldLabel("Icon shown"), 0, 2);
        layout.Controls.Add(_shown, 1, 2);
        layout.Controls.Add(_applyProfile, 1, 3);
        layout.Controls.Add(_profileNote, 1, 4);

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);

        _profileBox.SelectedIndexChanged += (_, _) => ShowProfile();
        _owned.ItemCheck += (_, _) => BeginInvoke(UpdateShownChoices);
        _applyProfile.Click += (_, _) => ApplyProfile();
        return page;
    }

    private void LoadProfiles()
    {
        var current = (_profileBox.SelectedItem as Profiles.Summary)?.Path;
        _profileBox.Items.Clear();
        foreach (var profile in _profiles.List())
        {
            _profileBox.Items.Add(profile);
        }

        _profileBox.SelectedItem = _profileBox.Items.Cast<Profiles.Summary>().FirstOrDefault(p => p.Path == current)
            ?? _profileBox.Items.Cast<Profiles.Summary>().FirstOrDefault(p => p.HasCharacter);
        if (_profileBox.Items.Count == 0)
        {
            _profileNote.Text = $"No profiles in {_profiles.Folder}";
        }
    }

    private void ShowProfile()
    {
        _owned.Items.Clear();
        if (_profileBox.SelectedItem is not Profiles.Summary profile)
        {
            return;
        }

        var choices = Categories.Grantable.Select(v => new CategoryItem(v, Categories.Label(v))).ToList();
        choices.InsertRange(0, _icons.Where(i => i.IsCustom).Select(i => new CategoryItem(i.Value, $"{DisplayName(i)}  (new, {i.Value})")));

        // Custom flags on the profile that icons.json doesn't draw any more
        for (var bit = Categories.FirstCustom; bit > 0; bit <<= 1)
        {
            if ((profile.MemberCategory & bit) != 0 && choices.All(c => c.Value != bit))
            {
                choices.Add(new CategoryItem(bit, $"Custom {bit}  (not in your icons)"));
            }
        }

        foreach (var choice in choices)
        {
            _owned.Items.Add(choice, (profile.MemberCategory & choice.Value) == choice.Value);
        }

        UpdateShownChoices();
        _shown.SelectedItem = _shown.Items.Cast<CategoryItem>().FirstOrDefault(c => c.Value == profile.Selected) ?? _shown.Items[0];

        _applyProfile.Enabled = profile.HasCharacter;
        _profileNote.Text = profile.HasCharacter
            ? "Close the SPT server before saving: it keeps the profile in memory and would save over the change. Start the server and the game afterwards."
            : "This profile has no character yet.";
    }

    private void UpdateShownChoices()
    {
        var previous = (_shown.SelectedItem as CategoryItem)?.Value;
        _shown.Items.Clear();
        _shown.Items.Add(new CategoryItem(0, "Standard (no icon)"));
        foreach (CategoryItem item in _owned.CheckedItems)
        {
            _shown.Items.Add(item);
        }

        _shown.SelectedItem = _shown.Items.Cast<CategoryItem>().FirstOrDefault(c => c.Value == previous) ?? _shown.Items[^1];
    }

    private void ApplyProfile()
    {
        if (_profileBox.SelectedItem is not Profiles.Summary profile)
        {
            return;
        }

        // Only the flags listed here change; anything else on the profile is left as it is
        var listed = _owned.Items.Cast<CategoryItem>().Aggregate(0, (mask, c) => mask | c.Value);
        var owned = _owned.CheckedItems.Cast<CategoryItem>().Aggregate(0, (mask, c) => mask | c.Value);
        var memberCategory = (profile.MemberCategory & ~listed) | owned;
        var selected = (_shown.SelectedItem as CategoryItem)?.Value ?? 0;

        try
        {
            var backup = _profiles.Write(profile, memberCategory, selected);
            LoadProfiles();
            SetStatus($"Profile saved (backup: {backup}).");
            _profileNote.Text = "Saved. Start the server, then the game." + (_dirty ? " Your icon changes aren't saved yet: press Save icons too." : "");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException or System.Text.Json.JsonException)
        {
            MessageBox.Show($"Not saved.\n\n{e.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ---------------------------------------------------------------- Helpers

    private static Label FieldLabel(string text) =>
        new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 6, 8, 0) };

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        row.Controls.AddRange(controls);
        return row;
    }

    private static Button Swatch() => new() { Size = new Size(34, 24), FlatStyle = FlatStyle.Flat, Text = "" };
}

/// <summary>
/// Roughly what the game shows: the icon and nickname by your name, and the settings dropdown.
/// </summary>
public class PreviewPanel : Control
{
    private Bitmap? _icon;
    private Bitmap? _dropdownIcon;
    private string _nickname = "";
    private string _label = "";
    private Color _color = Color.White;
    private bool _custom;

    public PreviewPanel()
    {
        DoubleBuffered = true;
        MinimumSize = new Size(300, 200);
    }

    public void SetPreview(Bitmap? icon, Bitmap? dropdownIcon, string nickname, string label, Color color, bool custom)
    {
        _icon = icon;
        _dropdownIcon = dropdownIcon;
        _nickname = nickname;
        _label = label;
        _color = Color.FromArgb(255, color);
        _custom = custom;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.FromArgb(14, 16, 17));
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;

        using var caption = new Font("Segoe UI", 8.5f);
        using var nameFont = new Font("Segoe UI Semibold", 15f);
        using var labelFont = new Font("Segoe UI", 10.5f);
        var gray = Color.FromArgb(140, 146, 150);
        var x = 18;

        TextRenderer.DrawText(g, "BY YOUR NAME", caption, new Point(x, 14), gray);
        DrawIcon(g, _icon, new Rectangle(x, 36, 34, 34));
        TextRenderer.DrawText(g, _nickname, nameFont, new Point(x + 44, 38), _color);

        TextRenderer.DrawText(g, "SETTINGS > GAME > PROFILE ICON", caption, new Point(x, 92), gray);
        using (var box = new SolidBrush(Color.FromArgb(30, 33, 35)))
        {
            g.FillRectangle(box, x, 112, 300, 30);
        }

        var labelX = x + 8;
        if (_dropdownIcon != null)
        {
            DrawIcon(g, _dropdownIcon, new Rectangle(labelX, 117, 20, 20));
            labelX += 26;
        }

        TextRenderer.DrawText(g, _label, labelFont, new Point(labelX, 117), _color);

        TextRenderer.DrawText(g, "LARGE", caption, new Point(x, 162), gray);
        DrawIcon(g, _icon, new Rectangle(x, 182, 96, 96));

        var note = _custom
            ? "New icons show without a picture in the settings dropdown; the game draws that from a fixed sprite sheet."
            : "The settings dropdown keeps the game's original small icon; it comes from a fixed sprite sheet.";
        TextRenderer.DrawText(g, note, caption, new Rectangle(x + 120, 190, Math.Max(100, Width - x - 140), 80), gray, TextFormatFlags.WordBreak);
    }

    private static void DrawIcon(Graphics g, Image? image, Rectangle rect)
    {
        if (image != null)
        {
            g.DrawImage(image, rect);
            return;
        }

        using var pen = new Pen(Color.FromArgb(90, 95, 100)) { DashStyle = DashStyle.Dash };
        g.DrawRectangle(pen, rect);
        TextRenderer.DrawText(g, "?", SystemFonts.DefaultFont, rect, Color.FromArgb(120, 125, 130), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
