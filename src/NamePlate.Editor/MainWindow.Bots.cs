using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NamePlate.Shared;

namespace NamePlate.Editor;

/// <summary>
/// The BOTS page: which looks PMC bots can get, previewed on real names from the installed
/// name mods, with the same generator the plugin runs.
/// </summary>
public partial class MainWindow
{
    private const int SampleCount = 14;

    private BotsConfig _bots = new();
    private BotNames.Pool _namePool = new([], new Dictionary<string, string>(), "");
    private readonly ObservableCollection<GalleryItem> _botIcons = [];
    private readonly ObservableCollection<BotSample> _samples = [];
    private readonly List<string> _sampleNames = [];
    private bool _botsLoading;

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

        TagSlider.ValueChanged += (_, _) => BotsEdit(b => b.TagChance = (int)Math.Round(TagSlider.Value));
        TagColorSlider.ValueChanged += (_, _) => BotsEdit(b => b.TagColorChance = (int)Math.Round(TagColorSlider.Value));

        NemesisSwitch.Checked += (_, _) => NemesisEdit(n => n.Enabled = true);
        NemesisSwitch.Unchecked += (_, _) => NemesisEdit(n => n.Enabled = false);
        NemesisSlider.ValueChanged += (_, _) => NemesisEdit(n => n.After = (int)Math.Round(NemesisSlider.Value));
        BotEffectSlider.ValueChanged += (_, _) => BotsEdit(b => b.EffectChance = (int)Math.Round(BotEffectSlider.Value));
        BotIconFxSlider.ValueChanged += (_, _) => BotsEdit(b => b.IconEffectChance = (int)Math.Round(BotIconFxSlider.Value));
        foreach (var toggle in BotEffectToggles)
        {
            toggle.Checked += (_, _) => BotsEdit(b => b.Effects = Picked(BotEffectToggles));
            toggle.Unchecked += (_, _) => BotsEdit(b => b.Effects = Picked(BotEffectToggles));
        }

        foreach (var toggle in BotIconEffectToggles)
        {
            toggle.Checked += (_, _) => BotsEdit(b => b.IconEffects = Picked(BotIconEffectToggles));
            toggle.Unchecked += (_, _) => BotsEdit(b => b.IconEffects = Picked(BotIconEffectToggles));
        }
        FactionSwitch.Checked += (_, _) => BotsEdit(b => b.FactionColors = true);
        FactionSwitch.Unchecked += (_, _) => BotsEdit(b => b.FactionColors = false);
        BotRandomColors.Checked += (_, _) => BotsEdit(b => b.RandomColors = true);
        BotRandomColors.Unchecked += (_, _) => BotsEdit(b => b.RandomColors = false);
        BotAnimateSlider.ValueChanged += (_, _) => BotsEdit(b => b.AnimateChance = (int)Math.Round(BotAnimateSlider.Value));
        BotSpeedSlider.ValueChanged += (_, _) => BotsEdit(b => b.MaxSpeed = Math.Round(BotSpeedSlider.Value, 2));
        foreach (var toggle in BotMotionToggles)
        {
            toggle.Checked += (_, _) => BotsEdit(ReadMotions);
            toggle.Unchecked += (_, _) => BotsEdit(ReadMotions);
        }

        BotPresetsAll.Click += (_, _) => SetAllPresets(true);
        BotPresetsNone.Click += (_, _) => SetAllPresets(false);

        BotIconGallery.SelectionChanged += (_, _) => BotsEdit(b =>
            b.Icons = BotIconGallery.SelectedItems.Cast<GalleryItem>().Select(BotIconEntry).OrderBy(e => e, StringComparer.Ordinal).ToList());
        BotIconsAll.Click += (_, _) => BotIconGallery.SelectAll();
        BotIconsNone.Click += (_, _) => BotIconGallery.UnselectAll();

        TryNameBox.TextChanged += (_, _) => RefreshKiller();
        ShuffleButton.Click += (_, _) => Shuffle();

        foreach (var (name, colors) in Presets)
        {
            var look = new NameLook(colors.Select(c => Images.ParseOr(c, Colors.White)).ToList(), NameMode.Gradient, 0);
            var toggle = new ToggleButton
            {
                Style = (Style)FindResource("ChipToggle"), Tag = colors,
                Content = new StyledName { Text = name, Look = look, FontSize = 13, FontWeight = FontWeights.SemiBold },
            };
            toggle.Checked += (_, _) => BotsEdit(ReadPalettes);
            toggle.Unchecked += (_, _) => BotsEdit(ReadPalettes);
            BotPresetPanel.Children.Add(toggle);
        }
    }

    private void LoadBots()
    {
        // Without a "bots" section the plugin already runs these defaults
        _bots = _file.Bots ?? DefaultBots();
        _bots.TakeInNew(BotDefaults.Icons(Pngs("icons/library")));
        _namePool = BotNames.Load(_sptPath);
        ShowBots();
        Shuffle();
    }

    /// <summary>What the plugin runs when icons.json has no "bots" section (BotDefaults).</summary>
    private BotsConfig DefaultBots() => new()
    {
        Enabled = true,
        Icons = [.. BotDefaults.Icons(Pngs("icons/library"))],
        Palettes = BotDefaults.Presets.Select(p => p.Colors.ToList()).ToList(),
        AnimateChance = BotDefaults.AnimateChance,
        MaxSpeed = BotDefaults.MaxSpeed,
    };

    private void ShowBots()
    {
        _botsLoading = true;
        try
        {
            BotsSwitch.IsChecked = _bots.Enabled;
            ShareSlider.Value = _bots.Share;
            TagSlider.Value = _bots.TagChance;
            TagColorSlider.Value = _bots.TagColorChance;
            var nemesis = _file.Nemesis ?? new IconsFile.NemesisConfig();
            NemesisSwitch.IsChecked = nemesis.Enabled;
            NemesisSlider.Value = nemesis.After;
            BotEffectSlider.Value = _bots.EffectChance;
            BotIconFxSlider.Value = _bots.IconEffectChance;
            foreach (var toggle in BotEffectToggles)
            {
                toggle.IsChecked = _bots.Effects.Contains((string)toggle.Tag);
            }

            foreach (var toggle in BotIconEffectToggles)
            {
                toggle.IsChecked = _bots.IconEffects.Contains((string)toggle.Tag);
            }
            FactionSwitch.IsChecked = _bots.FactionColors;
            BotSolid.IsChecked = _bots.Modes.Contains("solid");
            BotGradient.IsChecked = _bots.Modes.Contains("gradient");
            BotLetters.IsChecked = _bots.Modes.Contains("letters");
            BotRandomColors.IsChecked = _bots.RandomColors;
            BotAnimateSlider.Value = _bots.AnimateChance;
            BotSpeedSlider.Value = _bots.MaxSpeed;
            foreach (var toggle in BotMotionToggles)
            {
                toggle.IsChecked = _bots.Motions.Contains((string)toggle.Tag);
            }

            foreach (var toggle in BotPresetPanel.Children.OfType<ToggleButton>())
            {
                var colors = (string[])toggle.Tag;
                toggle.IsChecked = _bots.Palettes.Any(p => p.SequenceEqual(colors, StringComparer.OrdinalIgnoreCase));
            }

            // Library, the member icons and your own images; never the game's other icons
            _botIcons.Clear();
            foreach (var path in Pngs("icons/library").Concat(Pngs("icons")).Concat(Pngs("icons/imported")))
            {
                var item = new GalleryItem(System.IO.Path.GetFileNameWithoutExtension(path), path, null);
                item.SetFrames(ThumbnailFrames(System.IO.Path.Combine(_pluginFolder, path)));
                _botIcons.Add(item);
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
        }
        finally
        {
            _botsLoading = false;
        }

        UpdateBotTexts();
        RefreshBotPreview();
        EnsureFrameLoop();
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

    // The nemesis settings are the file's, not the bots section's: they count real players too
    private void NemesisEdit(Action<IconsFile.NemesisConfig> change)
    {
        if (_botsLoading)
        {
            return;
        }

        change(_file.Nemesis ??= new IconsFile.NemesisConfig());
        MarkDirty();
        UpdateBotTexts();
    }

    /// <summary>Who has killed you most, from the plugin's nemesis.json, each in the look they have in game.</summary>
    private void ShowNemesis()
    {
        var killers = new List<(string Name, int Deaths, string? Side)>();
        try
        {
            var path = System.IO.Path.Combine(_pluginFolder, "nemesis.json");
            if (System.IO.File.Exists(path))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("killers", out var list) && list.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var killer in list.EnumerateObject())
                    {
                        var deaths = killer.Value.TryGetProperty("deaths", out var d) && d.TryGetInt32(out var n) ? n : 0;
                        var side = killer.Value.TryGetProperty("side", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.String ? s.GetString() : null;
                        killers.Add((killer.Name, deaths, side));
                    }
                }
            }
        }
        catch (Exception e) when (e is System.Text.Json.JsonException or System.IO.IOException or UnauthorizedAccessException)
        {
            // Being written by the game, or not one of ours: show none this time
        }

        var after = (_file.Nemesis ?? new IconsFile.NemesisConfig()).After;
        var rows = killers.Where(k => k.Deaths > 0).OrderByDescending(k => k.Deaths).ThenBy(k => k.Name, StringComparer.OrdinalIgnoreCase).Take(8)
            .Select(k =>
            {
                var (look, icon, _) = PreviewBot(k.Name, k.Side);
                var count = k.Deaths == 1 ? "1 death" : $"{k.Deaths} deaths";
                return new BotSample(k.Name, look, icon, k.Deaths >= after ? $"NEMESIS · {count}" : count);
            })
            .ToList();
        NemesisList.ItemsSource = rows;
        NemesisNone.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private ToggleButton[] BotEffectToggles => [BotFxOutline, BotFxGlow, BotFxShadow, BotFxBold, BotFxItalic, BotFxCaps];

    private ToggleButton[] BotIconEffectToggles => [BotIconFxPulse, BotIconFxGlow, BotIconFxFlip, BotIconFxFloat];

    private static List<string> Picked(ToggleButton[] toggles) => toggles.Where(t => t.IsChecked == true).Select(t => (string)t.Tag).ToList();

    private static string EffectName(string effect) => effect == NameEffectKinds.SmallCaps ? "Small caps" : char.ToUpperInvariant(effect[0]) + effect[1..];

    private ToggleButton[] BotMotionToggles => [BotMotionScroll, BotMotionPulse, BotMotionWave, BotMotionSparkle, BotMotionRainbow, BotMotionGlitch, BotMotionBounce];

    private void ReadMotions(BotsConfig bots) =>
        bots.Motions = BotMotionToggles.Where(t => t.IsChecked == true).Select(t => (string)t.Tag).ToList();

    // One edit for all of them, not one per toggle
    private void SetAllPresets(bool on)
    {
        _botsLoading = true;
        foreach (var toggle in BotPresetPanel.Children.OfType<ToggleButton>())
        {
            toggle.IsChecked = on;
        }

        _botsLoading = false;
        BotsEdit(ReadPalettes);
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
        BotEffectText.Text = $"{_bots.EffectChance}% of names";
        BotIconFxText.Text = $"{_bots.IconEffectChance}% of icons";
        TagText.Text = $"{_bots.TagChance}% of them · {(_bots.Tags ?? [.. BotTags.All]).Count} tags";
        TagColorText.Text = $"{_bots.TagColorChance}% of tags";
        var after = (_file.Nemesis ?? new IconsFile.NemesisConfig()).After;
        NemesisText.Text = after == 1 ? "the first death" : $"{after} deaths to the same PMC";
        BotAnimateText.Text = $"{_bots.AnimateChance}% of names";
        BotSpeedText.Text = $"{_bots.MaxSpeed:0.00} loops/s";
        var presetsOn = BotPresetPanel.Children.OfType<ToggleButton>().Count(t => t.IsChecked == true);
        BotPresetCount.Text = $"{presetsOn} of {Presets.Length} on";
        BotIconCount.Text = _bots.Icons.Count == 0
            ? "None picked: bots keep the game's icon."
            : $"{_bots.Icons.Count} icons in the pool. Click to add or take one out.";
        NameSourceText.Text = _namePool.Names.Count > 0 ? $"{_namePool.Names.Count:N0} names from {_namePool.Source}" : "No PMC names found";
    }

    // ---------------------------------------------------------------- Preview

    private void Shuffle()
    {
        _sampleNames.Clear();
        var pool = _namePool.Names;
        foreach (var index in Enumerable.Range(0, pool.Count).OrderBy(_ => _random.Next()).Take(SampleCount))
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
            "Plain" => _bots.Enabled ? "This name gets no look (Share of PMCs)." : "PMC bot looks are off.",
            _ => "",
        };
    }

    /// <summary>
    /// What the plugin will draw for a PMC bot of this name on the death screen. Names from the
    /// pool are on the side their list is for, so faction colors show; any other name has no side.
    /// </summary>
    private (NameLook Look, BitmapSource? Icon, string Tag) PreviewBot(string name, string? side = null)
    {
        var plainIcon = Images.Load(System.IO.Path.Combine(_pluginFolder, _game.MemberIconFile(0) ?? "-"));
        var plain = (NameLook.Solid(Images.ParseOr(StandardGray, Colors.Gray)), plainIcon, "Plain");
        if (!_bots.Enabled)
        {
            return plain;
        }

        var look = BotLookGenerator.For(name, _bots.ToRules(), side ?? _namePool.Sides.GetValueOrDefault(name));
        if (look == null)
        {
            return plain;
        }

        var colors = look.Colors.Select(c => Images.ParseOr(c, Colors.White)).ToList();
        var mode = look.Mode switch { "gradient" => NameMode.Gradient, "letters" => NameMode.Letters, _ => NameMode.Solid };
        var effectColor = look.EffectColor != null ? Images.ParseOr(look.EffectColor, Colors.Black) : (Color?)null;
        NameFx? fx = look.Effect switch
        {
            NameEffectKinds.Outline => new NameFx(Outline: effectColor),
            NameEffectKinds.Glow => new NameFx(Glow: effectColor),
            NameEffectKinds.Shadow => new NameFx(Shadow: true),
            NameEffectKinds.Bold => new NameFx(Bold: true),
            NameEffectKinds.Italic => new NameFx(Italic: true),
            NameEffectKinds.SmallCaps => new NameFx(SmallCaps: true),
            _ => null,
        };
        var nameLook = new NameLook(colors, mode, look.Speed, look.Motion, look.Reverse, fx, look.Tag, look.TagColor);

        BitmapSource? icon = plainIcon;
        if (look.Icon is { } entry)
        {
            var file = entry.StartsWith(BotLookGenerator.MemberPrefix, StringComparison.OrdinalIgnoreCase)
                ? _game.MemberIconFile(Categories.Parse(entry[BotLookGenerator.MemberPrefix.Length..]) ?? 0)
                : entry;
            icon = file == null ? plainIcon : Images.Load(System.IO.Path.Combine(_pluginFolder, file)) is { } picture ? Images.FirstFrame(picture) : null;
        }

        var tag = mode switch { NameMode.Gradient => "Gradient", NameMode.Letters => "Per letter", _ => "One color" }
            + (look.Motion != null ? " · " + char.ToUpperInvariant(look.Motion[0]) + look.Motion[1..] + (look.Reverse ? " ↺" : "") : "")
            + (look.Effect != null ? " · " + EffectName(look.Effect) : "")
            + (look.IconEffect != null ? " · icon " + look.IconEffect : "");
        return (nameLook, icon, tag);
    }
}

public record BotSample(string DisplayName, NameLook Look, ImageSource? Thumbnail, string Tag);
