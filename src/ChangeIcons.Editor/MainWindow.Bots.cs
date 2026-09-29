using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChangeIcons.Shared;

namespace ChangeIcons.Editor;

/// <summary>
/// The BOTS page: which looks PMC bots can get, previewed on real names from the installed
/// name mods, with the same generator the plugin runs.
/// </summary>
public partial class MainWindow
{
    private const int SampleCount = 14;

    private BotsConfig _bots = new();
    private BotNames.Pool _namePool = new([], "");
    private readonly ObservableCollection<GalleryItem> _botIcons = [];
    private readonly ObservableCollection<BotSample> _samples = [];
    private readonly List<string> _sampleNames = [];
    private readonly List<NamedRow> _namedRows = [];
    private readonly Random _shuffle = new();
    private bool _botsLoading;

    private class NamedRow
    {
        public required TextBox Box;
        public required Button Pick;
        public required string Key;
    }

    private void WireBots()
    {
        BotIconGallery.ItemsSource = _botIcons;
        BotSampleList.ItemsSource = _samples;

        BotsSwitch.Checked += (_, _) => BotsEdit(b => b.Enabled = true);
        BotsSwitch.Unchecked += (_, _) => BotsEdit(b => b.Enabled = false);
        ShareSlider.ValueChanged += (_, _) => BotsEdit(b => b.Share = (int)Math.Round(ShareSlider.Value));
        foreach (var toggle in new[] { BotSolid, BotGradient, BotLetters })
        {
            toggle.Checked += (_, _) => BotsEdit(ReadModes);
            toggle.Unchecked += (_, _) =>
            {
                // Bots need at least one way to color a name
                if (!_botsLoading && BotSolid.IsChecked != true && BotGradient.IsChecked != true && BotLetters.IsChecked != true)
                {
                    toggle.IsChecked = true;
                    return;
                }

                BotsEdit(ReadModes);
            };
        }

        BotRandomColors.Checked += (_, _) => BotsEdit(b => b.RandomColors = true);
        BotRandomColors.Unchecked += (_, _) => BotsEdit(b => b.RandomColors = false);
        BotAnimateSlider.ValueChanged += (_, _) => BotsEdit(b => b.AnimateChance = (int)Math.Round(BotAnimateSlider.Value));
        BotSpeedSlider.ValueChanged += (_, _) => BotsEdit(b => b.MaxSpeed = Math.Round(BotSpeedSlider.Value, 2));

        BotIconGallery.SelectionChanged += (_, _) => BotsEdit(b =>
            b.Icons = BotIconGallery.SelectedItems.Cast<GalleryItem>().Select(BotIconEntry).OrderBy(e => e, StringComparer.Ordinal).ToList());
        BotIconsAll.Click += (_, _) => BotIconGallery.SelectAll();
        BotIconsNone.Click += (_, _) => BotIconGallery.UnselectAll();

        AddNamedButton.Click += (_, _) =>
        {
            AddNamedRow("", _items.FirstOrDefault(i => i.Icon.IsCustom)?.Icon.Key ?? "Unheard");
            _namedRows[^1].Box.Focus();
        };
        TryNameBox.TextChanged += (_, _) => RefreshKiller();
        ShuffleButton.Click += (_, _) => Shuffle();

        foreach (var (name, colors) in Presets)
        {
            var look = new NameLook(colors.Select(c => Images.ParseOr(c, Colors.White)).ToList(), NameMode.Gradient, 0);
            var toggle = new ToggleButton
            {
                Style = (Style)FindResource("ChipToggle"), Tag = colors,
                Content = new StyledName { Text = name.ToUpperInvariant(), Look = look, FontSize = 14, FontWeight = FontWeights.Bold },
            };
            toggle.Checked += (_, _) => BotsEdit(ReadPalettes);
            toggle.Unchecked += (_, _) => BotsEdit(ReadPalettes);
            BotPresetPanel.Children.Add(toggle);
        }
    }

    private void LoadBots()
    {
        _bots = _file.Bots ?? DefaultBots();
        _namePool = BotNames.Load(_sptPath);
        ShowBots();
        Shuffle();

        // Defaults the plugin hasn't been told about yet: SAVE writes them
        if (_file.Bots == null)
        {
            MarkDirty();
            SetStatus("PMC bot looks are new: press SAVE to turn them on");
        }
    }

    /// <summary>Everything on: every mode, preset, library and member icon.</summary>
    private BotsConfig DefaultBots() => new()
    {
        Enabled = true,
        Icons = Pngs("icons/library")
            .Concat(Categories.Grantable.Select(v => BotLookGenerator.MemberPrefix + Categories.EnumName(v)))
            .OrderBy(e => e, StringComparer.Ordinal)
            .ToList(),
        Palettes = Presets.Select(p => p.Colors.ToList()).ToList(),
    };

    private void ShowBots()
    {
        _botsLoading = true;
        try
        {
            BotsSwitch.IsChecked = _bots.Enabled;
            ShareSlider.Value = _bots.Share;
            BotSolid.IsChecked = _bots.Modes.Contains("solid");
            BotGradient.IsChecked = _bots.Modes.Contains("gradient");
            BotLetters.IsChecked = _bots.Modes.Contains("letters");
            BotRandomColors.IsChecked = _bots.RandomColors;
            BotAnimateSlider.Value = _bots.AnimateChance;
            BotSpeedSlider.Value = _bots.MaxSpeed;

            foreach (var toggle in BotPresetPanel.Children.OfType<ToggleButton>())
            {
                var colors = (string[])toggle.Tag;
                toggle.IsChecked = _bots.Palettes.Any(p => p.SequenceEqual(colors, StringComparer.OrdinalIgnoreCase));
            }

            // Library, the member icons and your own images; never the game's other icons
            _botIcons.Clear();
            foreach (var path in Pngs("icons/library").Concat(Pngs("icons")).Concat(Pngs("icons/imported")))
            {
                _botIcons.Add(new GalleryItem(System.IO.Path.GetFileNameWithoutExtension(path), path, null) { Thumbnail = Thumbnail(System.IO.Path.Combine(_pluginFolder, path)) });
            }

            foreach (var value in Categories.Grantable)
            {
                if (_game.MemberIconFile(value) is { } file)
                {
                    _botIcons.Add(new GalleryItem(Categories.Label(value), file, value) { Thumbnail = Thumbnail(System.IO.Path.Combine(_pluginFolder, file)) });
                }
            }

            BotIconGallery.UnselectAll();
            foreach (var item in _botIcons.Where(i => _bots.Icons.Contains(BotIconEntry(i))))
            {
                BotIconGallery.SelectedItems.Add(item);
            }

            NamedPanel.Children.Clear();
            _namedRows.Clear();
            foreach (var (name, key) in _bots.Names)
            {
                AddNamedRow(name, key);
            }
        }
        finally
        {
            _botsLoading = false;
        }

        UpdateBotTexts();
        RefreshBotPreview();
    }

    private static string BotIconEntry(GalleryItem item) =>
        item.Member is { } member ? BotLookGenerator.MemberPrefix + Categories.EnumName(member) : item.Path;

    private void BotsEdit(Action<BotsConfig> change)
    {
        if (_botsLoading)
        {
            return;
        }

        change(_bots);
        MarkDirty();
        UpdateBotTexts();
        RefreshBotPreview();
    }

    private void ReadModes(BotsConfig bots)
    {
        bots.Modes = [];
        if (BotSolid.IsChecked == true)
        {
            bots.Modes.Add("solid");
        }

        if (BotGradient.IsChecked == true)
        {
            bots.Modes.Add("gradient");
        }

        if (BotLetters.IsChecked == true)
        {
            bots.Modes.Add("letters");
        }
    }

    // Presets that are on, plus any palette in icons.json that isn't one of the presets
    private void ReadPalettes(BotsConfig bots)
    {
        var presets = BotPresetPanel.Children.OfType<ToggleButton>().ToList();
        var extra = bots.Palettes.Where(p => !presets.Any(t => p.SequenceEqual((string[])t.Tag, StringComparer.OrdinalIgnoreCase)));
        bots.Palettes = presets.Where(t => t.IsChecked == true).Select(t => ((string[])t.Tag).ToList()).Concat(extra).ToList();
    }

    private void UpdateBotTexts()
    {
        ShareText.Text = $"{_bots.Share}%";
        BotAnimateText.Text = $"{_bots.AnimateChance}% of multi-color names";
        BotSpeedText.Text = $"{_bots.MaxSpeed:0.00} loops/s";
        BotIconCount.Text = _bots.Icons.Count == 0
            ? "None picked: bots keep the game's icon."
            : $"{_bots.Icons.Count} icons in the pool. Click to add or take one out.";
        NameSourceText.Text = _namePool.Names.Count > 0 ? $"{_namePool.Names.Count:N0} names from {_namePool.Source}" : "No PMC names found";
    }

    // ---------------------------------------------------------------- Bots with a fixed look

    private void AddNamedRow(string name, string key)
    {
        var box = new TextBox { Text = name, Width = 240, Tag = "Bot name", Margin = new Thickness(0, 0, 10, 0) };
        var pick = new Button { Padding = new Thickness(10, 5, 10, 5), MinWidth = 200, HorizontalContentAlignment = HorizontalAlignment.Left };
        var remove = new Button { Content = "✕", Style = (Style)FindResource("GhostBtn"), Margin = new Thickness(6, 0, 0, 0), ToolTip = "Remove" };
        var row = new NamedRow { Box = box, Pick = pick, Key = key };

        pick.Content = IconChoice(key);
        pick.Click += (_, _) =>
        {
            var menu = new ContextMenu { PlacementTarget = pick, Placement = PlacementMode.Bottom };
            foreach (var item in _items)
            {
                var entry = new MenuItem { Header = IconChoice(item.Icon.Key) };
                var chosen = item.Icon.Key;
                entry.Click += (_, _) =>
                {
                    row.Key = chosen;
                    pick.Content = IconChoice(chosen);
                    BotsEdit(ReadNames);
                };
                menu.Items.Add(entry);
            }

            menu.IsOpen = true;
        };
        box.TextChanged += (_, _) => BotsEdit(ReadNames);

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(box);
        panel.Children.Add(pick);
        panel.Children.Add(remove);
        remove.Click += (_, _) =>
        {
            NamedPanel.Children.Remove(panel);
            _namedRows.Remove(row);
            BotsEdit(ReadNames);
        };

        NamedPanel.Children.Add(panel);
        _namedRows.Add(row);
    }

    private FrameworkElement IconChoice(string key)
    {
        var icon = _items.FirstOrDefault(i => i.Icon.Key == key)?.Icon;
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new Image { Source = icon != null ? IconImage(icon) : null, Width = 18, Height = 18, Margin = new Thickness(0, 0, 8, 0) });
        panel.Children.Add(new StyledName
        {
            Text = icon != null ? DisplayName(icon) : $"{key} (gone)",
            Look = icon != null ? Look(icon) : NameLook.Solid(Colors.Gray),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return panel;
    }

    private void ReadNames(BotsConfig bots)
    {
        bots.Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in _namedRows.Where(r => !string.IsNullOrWhiteSpace(r.Box.Text)))
        {
            bots.Names[row.Box.Text.Trim()] = row.Key;
        }
    }

    // ---------------------------------------------------------------- Preview

    private void Shuffle()
    {
        _sampleNames.Clear();
        var pool = _namePool.Names;
        foreach (var index in Enumerable.Range(0, pool.Count).OrderBy(_ => _shuffle.Next()).Take(SampleCount))
        {
            _sampleNames.Add(pool[index]);
        }

        RefreshBotPreview();
    }

    private void RefreshBotPreview()
    {
        _samples.Clear();
        foreach (var name in _sampleNames)
        {
            var (look, icon, tag) = PreviewBot(name);
            _samples.Add(new BotSample(name, look, icon, tag));
        }

        RefreshKiller();
    }

    private void RefreshKiller()
    {
        var name = string.IsNullOrWhiteSpace(TryNameBox.Text) ? _sampleNames.FirstOrDefault() ?? "Nickname" : TryNameBox.Text.Trim();
        var (look, icon, tag) = PreviewBot(name);
        KillerName.Text = name;
        KillerName.Look = look;
        KillerIcon.Source = icon;
        RenderOptions.SetBitmapScalingMode(KillerIcon, icon is { PixelWidth: < 48 } ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        KillerNote.Text = tag switch
        {
            "PLAIN" => _bots.Enabled ? "This name gets no look (Share of PMCs)." : "PMC bot looks are off.",
            "FIXED" => "A fixed look you gave this name.",
            _ => "",
        };
    }

    /// <summary>What the plugin will draw for a PMC bot of this name.</summary>
    private (NameLook Look, BitmapSource? Icon, string Tag) PreviewBot(string name)
    {
        var plainIcon = Images.Load(System.IO.Path.Combine(_pluginFolder, _game.MemberIconFile(0) ?? "-"));
        var plain = (NameLook.Solid(Images.ParseOr(StandardGray, Colors.Gray)), plainIcon, "PLAIN");
        if (!_bots.Enabled)
        {
            return plain;
        }

        var named = _bots.Names.FirstOrDefault(n => string.Equals(n.Key, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (named.Key != null && _items.FirstOrDefault(i => i.Icon.Key == named.Value)?.Icon is { } fixedIcon)
        {
            return (Look(fixedIcon), IconImage(fixedIcon), "FIXED");
        }

        var look = BotLookGenerator.For(name, _bots.ToRules());
        if (look == null)
        {
            return plain;
        }

        var colors = look.Colors.Select(c => Images.ParseOr(c, Colors.White)).ToList();
        var mode = look.Mode switch { "gradient" => NameMode.Gradient, "letters" => NameMode.Letters, _ => NameMode.Solid };
        var nameLook = mode == NameMode.Solid ? NameLook.Solid(colors[0]) : new NameLook(colors, mode, look.Speed);

        BitmapSource? icon = plainIcon;
        if (look.Icon is { } entry)
        {
            var file = entry.StartsWith(BotLookGenerator.MemberPrefix, StringComparison.OrdinalIgnoreCase)
                ? _game.MemberIconFile(Categories.Parse(entry[BotLookGenerator.MemberPrefix.Length..]) ?? 0)
                : entry;
            icon = file == null ? plainIcon : Images.Load(System.IO.Path.Combine(_pluginFolder, file));
        }

        var tag = mode switch { NameMode.Gradient => "GRADIENT", NameMode.Letters => "PER LETTER", _ => "ONE COLOR" } + (look.Speed > 0 ? " · MOVING" : "");
        return (nameLook, icon, tag);
    }
}

public record BotSample(string DisplayName, NameLook Look, ImageSource? Thumbnail, string Tag);
