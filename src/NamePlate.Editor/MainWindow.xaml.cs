using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NamePlate.Editor;

public partial class MainWindow : Window
{
    // Shared with the plugin, whose bot defaults use them
    private static readonly (string Name, string[] Colors)[] Presets =
        Shared.BotDefaults.Presets.Select(p => (p.Name, p.Colors)).ToArray();

    private const string StandardGray = "#C3CDD3";

    private readonly string _sptPath;
    private readonly string _pluginFolder;
    private readonly string _iconsJson;
    private readonly GameAssets _game;
    private readonly Profiles _profiles;
    private readonly ObservableCollection<IconItem> _items = [];
    private readonly ObservableCollection<GalleryItem> _gallery = [];
    private readonly ObservableCollection<ProfileItem> _profileItems = [];
    private readonly DispatcherTimer _serverTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private IconsFile _file = new();

    // Entries in icons.json the editor can't show (a category it doesn't know). Written back as
    // they were: the plugin may still read them, and dropping them would lose someone's work.
    private readonly List<IconsFile.Entry> _kept = [];

    // icons.json is copied to backups\ before this window first saves over it
    private bool _backedUp;
    private CancellationTokenSource? _thumbLoad;
    private string _nickname = "Nickname";
    private readonly Random _random = new();

    // The preview's animated icon: frames and effect, played on the frame loop (MainWindow.Extras)
    private IReadOnlyList<BitmapSource> _iconFrames = [];
    private EditableIcon? _iconPlaying;
    private static readonly DateTime IconClock = DateTime.UtcNow;
    private bool _loading;
    private bool _dirty;

    private EditableIcon? Current => (IconList.SelectedItem as IconItem)?.Icon;

    public MainWindow(string sptPath)
    {
        InitializeComponent();

        _sptPath = sptPath;
        _pluginFolder = Path.Combine(sptPath, "BepInEx", "plugins", "NamePlate");
        _iconsJson = Path.Combine(_pluginFolder, "icons.json");
        _game = new GameAssets(sptPath, _pluginFolder);
        _profiles = new Profiles(sptPath, Path.Combine(_pluginFolder, "backups"));

        IconList.ItemsSource = _items;
        Gallery.ItemsSource = _gallery;
        ProfileList.ItemsSource = _profileItems;

        SourceInitialized += (_, _) => DarkTitleBar();
        Loaded += async (_, _) => await Start();
        Closing += OnClosing;

        IconsTab.Checked += (_, _) => ShowPage();
        BotsTab.Checked += (_, _) => ShowPage();
        ProfileTab.Checked += (_, _) => ShowPage();
        SaveButton.Click += (_, _) => Save();
        // Just the folder's name: a full path can be long enough to run into the tabs
        SptButton.Content = $"SPT: {new DirectoryInfo(sptPath).Name}";
        SptButton.ToolTip = sptPath + Environment.NewLine + "Click to pick a different SPT folder";
        SptButton.Click += (_, _) =>
        {
            if (App.PickSpt(this) is { } other && !string.Equals(Path.GetFullPath(other), Path.GetFullPath(_sptPath), StringComparison.OrdinalIgnoreCase))
            {
                ((App)Application.Current).SwitchTo(other);
            }
        };

        IconList.SelectionChanged += (_, _) => ShowCurrent();
        NewButton.Click += (_, _) => AddIcon(copyOf: null);
        CopyButton.Click += (_, _) => AddIcon(copyOf: Current);
        RemoveButton.Click += (_, _) => RemoveCurrent();

        NameBox.TextChanged += (_, _) => Edit(i => i.Name = string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim(), undoGroup: "name");
        SolidMode.Checked += (_, _) => SetMode(NameMode.Solid);
        GradientMode.Checked += (_, _) => SetMode(NameMode.Gradient);
        LettersMode.Checked += (_, _) => SetMode(NameMode.Letters);
        GameColorButton.Click += (_, _) => Edit(i =>
        {
            i.Colors.Clear();
            i.Mode = NameMode.Solid;
            i.Animate = 0;
        }, reshow: true);
        foreach (var (radio, motion) in new[]
                 {
                     (MotionScroll, Shared.Motions.Scroll), (MotionPulse, Shared.Motions.Pulse), (MotionWave, Shared.Motions.Wave), (MotionSparkle, Shared.Motions.Sparkle),
                     (MotionRainbow, Shared.Motions.Rainbow), (MotionGlitch, Shared.Motions.Glitch), (MotionBounce, Shared.Motions.Bounce),
                 })
        {
            radio.Checked += (_, _) => Edit(i => i.Motion = motion);
        }

        TagBox.TextChanged += (_, _) =>
        {
            Edit(i => i.Tag = CleanTag(TagBox.Text), undoGroup: "tag");
            TagColorRow.Visibility = CleanTag(TagBox.Text) != null ? Visibility.Visible : Visibility.Collapsed;
        };
        RandomTagButton.Click += (_, _) => TagBox.Text = Shared.BotTags.All[_random.Next(Shared.BotTags.All.Length)];
        TagGray.Checked += (_, _) => Edit(i => i.TagColor = null, reshow: true);
        TagName.Checked += (_, _) => Edit(i => i.TagColor = Shared.TagColors.Name, reshow: true);
        TagOwn.Checked += (_, _) => Edit(i =>
        {
            if (!Images.TryParseColor(i.TagColor, out _))
            {
                i.TagColor = FirstColor(i);
            }
        }, reshow: true);
        TagColorChip.Click += (_, _) => PickEffectColor(TagColorChip, i => i.TagColor, (i, c) => i.TagColor = c);
        SurpriseButton.Click += (_, _) => Surprise();
        CopyCodeButton.Click += (_, _) => CopyCode();
        PasteCodeButton.Click += (_, _) => PasteCode();

        OutlineSwitch.Checked += (_, _) => Edit(i => i.Outline ??= "#000000", reshow: true);
        OutlineSwitch.Unchecked += (_, _) => Edit(i => i.Outline = null, reshow: true);
        OutlineChip.Click += (_, _) => PickEffectColor(OutlineChip, i => i.Outline, (i, c) => i.Outline = c);
        GlowSwitch.Checked += (_, _) => Edit(i => { i.Glow ??= FirstColor(i); i.Shadow = false; }, reshow: true);
        GlowSwitch.Unchecked += (_, _) => Edit(i => i.Glow = null, reshow: true);
        GlowChip.Click += (_, _) => PickEffectColor(GlowChip, i => i.Glow, (i, c) => i.Glow = c);
        ShadowSwitch.Checked += (_, _) => Edit(i => { i.Shadow = true; i.Glow = null; }, reshow: true);
        ShadowSwitch.Unchecked += (_, _) => Edit(i => i.Shadow = false, reshow: true);
        ItalicToggle.Checked += (_, _) => Edit(i => i.Italic = true);
        ItalicToggle.Unchecked += (_, _) => Edit(i => i.Italic = false);

        foreach (var (radio, effect) in new[] { (EffectNone, (string?)null), (EffectPulse, "pulse"), (EffectGlow, "glow"), (EffectFlip, "flip"), (EffectFloat, "float") })
        {
            radio.Checked += (_, _) => Edit(i => i.IconEffect = effect);
        }

        PrestigeGame.Checked += (_, _) => Edit(i => i.Prestige = null);
        PrestigeHide.Checked += (_, _) => Edit(i => i.Prestige = "hide");
        FpsSlider.ValueChanged += (_, _) =>
        {
            FpsText.Text = $"{FpsSlider.Value:0} frames/s";
            Edit(i => i.Fps = Math.Round(FpsSlider.Value), undoGroup: "fps");
        };
        Closed += (_, _) => StopFrameLoop();

        ScavSwitch.Checked += (_, _) => SetFileOption(f => f.ScavLook = true);
        ScavSwitch.Unchecked += (_, _) => SetFileOption(f => f.ScavLook = false);
        ShareSwitch.Checked += (_, _) => SetFileOption(f => f.ShareNamePlate = true);
        ShareSwitch.Unchecked += (_, _) => SetFileOption(f => f.ShareNamePlate = false);

        ReverseSwitch.Checked += (_, _) => Edit(i => i.Reverse = true);
        ReverseSwitch.Unchecked += (_, _) => Edit(i => i.Reverse = false);
        AnimateSwitch.Checked += (_, _) => Edit(i => i.Animate = SpeedSlider.Value);
        AnimateSwitch.Unchecked += (_, _) => Edit(i => i.Animate = 0);
        SpeedSlider.ValueChanged += (_, _) =>
        {
            UpdateSpeedText();
            if (AnimateSwitch.IsChecked == true)
            {
                Edit(i => i.Animate = SpeedSlider.Value, undoGroup: "speed");
            }
        };

        TintSwitch.Checked += (_, _) => Edit(i => i.Tint ??= FirstColor(i), reshow: true);
        TintSwitch.Unchecked += (_, _) => Edit(i => i.Tint = null, reshow: true);
        TintChip.Click += (_, _) =>
        {
            if (Current is { } icon)
            {
                ColorPicker.Open(TintChip, Images.ParseOr(icon.Tint, Colors.White), c => Edit(i => i.Tint = Images.ToHex(c), reshow: true, undoGroup: "tint"));
            }
        };
        OwnIconButton.Click += (_, _) => Edit(i =>
        {
            i.Source = IconSource.Own;
            i.GameIcon = i.File = null;
        }, reshow: true);

        foreach (var tab in new[] { LibraryTab, AnimatedTab, GameTab, MineTab })
        {
            tab.Checked += (_, _) => ShowGallery();
        }

        SearchBox.TextChanged += (_, _) => ShowGallery();
        SearchBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                SearchBox.Clear();
                e.Handled = true;
            }
        };

        // Ctrl+S saves anywhere; Ctrl+Z undoes icon changes (a text box keeps Ctrl+Z for its own typing)
        KeyDown += (_, e) =>
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            if (e.Key == Key.S)
            {
                if (SaveButton.IsEnabled)
                {
                    Save();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Z && IconsPage.Visibility == Visibility.Visible)
            {
                Undo();
                e.Handled = true;
            }
        };
        LargeSwitch.Checked += (_, _) => ShowGallery();
        LargeSwitch.Unchecked += (_, _) => ShowGallery();
        RereadButton.Click += async (_, _) => await ReadGame();
        Gallery.SelectionChanged += (_, _) => PickFromGallery();

        ProfileList.SelectionChanged += (_, _) => ShowProfile();
        ApplyProfileButton.Click += (_, _) => ApplyProfile();
        _serverTimer.Tick += (_, _) => UpdateServerState();

        BuildPresets();
        WireBots();
    }

    private async Task Start()
    {
        // The DLLs, not the folders: the editor itself creates the plugin folder for game-icons
        var missing = new List<string>();
        if (!File.Exists(Path.Combine(_pluginFolder, "NamePlate.Client.dll")))
        {
            missing.Add(@"BepInEx\plugins\NamePlate\NamePlate.Client.dll (icons, colors, bots)");
        }

        if (!File.Exists(Path.Combine(_sptPath, "SPT_Runtime", "user", "mods", "NamePlate", "NamePlate.dll")))
        {
            missing.Add(@"SPT_Runtime\user\mods\NamePlate\NamePlate.dll (server mod)");
        }

        if (missing.Count > 0)
        {
            MessageBox.Show(this,
                $"This SPT folder is missing part of the mod:\n\n{string.Join("\n", missing)}\n\n"
                + $"Unpack the whole Name Plate zip into {_sptPath} (it adds those files next to this editor), "
                + "then restart the game. You can keep editing meanwhile; your changes are saved for when it's installed."
                + (missing.Count == 1 && missing[0].StartsWith("SPT_Runtime")
                    ? "\n\nPlaying on a Fika host's server? Then the server mod belongs on the host, and this is fine. "
                      + "A Name Plate made on the host reaches you; one you make here, for the same icon, wins."
                    : ""),
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        if (!_game.Extracted || _game.Stale)
        {
            await ReadGame();
        }

        // Each part on its own: one that fails (a profile or another mod's file the editor
        // doesn't expect) costs that part, not the editor
        StartStep("your profiles", () => _nickname = _profiles.List().FirstOrDefault(p => p.HasCharacter)?.Nickname ?? "Nickname");
        SetStatus("");
        StartStep("icons.json", LoadIcons);
        StartStep("the bot settings", LoadBots);
        StartStep("the icon gallery", ShowGallery);
    }

    private void StartStep(string what, Action step)
    {
        try
        {
            step();
        }
        catch (Exception e)
        {
            App.WriteCrash(e);
            MessageBox.Show(this, $"Couldn't load {what}; the rest of the editor works.\n\n{e.GetType().Name}: {e.Message}\n\n"
                + $"The details are in {App.CrashLogPath}. Please attach that file to a bug report.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---------------------------------------------------------------- Reading the game

    private async Task ReadGame()
    {
        BusyOverlay.Visibility = Visibility.Visible;
        BusyText.Text = "Opening resources.assets...";
        var progress = new Progress<string>(text => BusyText.Text = text);
        try
        {
            await Task.Run(() => _game.Extract(progress));
            SetStatus($"Read {_game.Sprites.Count} icons from the game");
        }
        catch (Exception e)
        {
            MessageBox.Show(this, $"Couldn't read the game's icons.\n\n{e.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BusyOverlay.Visibility = Visibility.Collapsed;
        }

        foreach (var item in _items)
        {
            Refresh(item);
        }

        ShowGallery();
        UpdatePreview();
    }

    // ---------------------------------------------------------------- Icon list

    private void LoadIcons()
    {
        var icons = new List<EditableIcon>();
        _kept.Clear();
        try
        {
            _file = IconsFile.Load(_iconsJson);
        }
        catch (Exception e)
        {
            App.WriteCrash(e);
            MessageBox.Show(this, $"Couldn't read icons.json, starting fresh. Saving keeps a copy of the old one in {Path.Combine(_pluginFolder, "backups")}.\n\n{e.Message}",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            _file = new IconsFile();
        }

        // One entry the editor can't read costs that entry, not the file
        foreach (var entry in _file.Icons)
        {
            try
            {
                icons.Add(IconsFile.ToEditable(entry));
            }
            catch (FormatException)
            {
                _kept.Add(entry);
            }
        }

        if (_kept.Count > 0)
        {
            MessageBox.Show(this,
                $"icons.json has {_kept.Count} icon(s) this editor can't show: {string.Join(", ", _kept.Select(k => $"\"{k.Category}\""))}.\n\n"
                + "They're kept in the file as they are when you save.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _settingOptions = true;
        ScavSwitch.IsChecked = _file.ScavLook;
        ShareSwitch.IsChecked = _file.ShareNamePlate;
        _settingOptions = false;
        BuildPresets();

        foreach (var value in Categories.WithIcons.Where(v => icons.All(i => i.Value != v)))
        {
            icons.Add(new EditableIcon { Value = value });
        }

        _items.Clear();
        foreach (var icon in icons.OrderBy(i => i.IsCustom ? 0 : 1).ThenBy(i => i.Value))
        {
            var item = new IconItem(icon);
            Refresh(item);
            _items.Add(item);
        }

        IconList.SelectedIndex = 0;
    }

    private void Refresh(IconItem item) =>
        item.Update(DisplayName(item.Icon), Look(item.Icon) with { Tag = null }, IconImage(item.Icon),
            item.Icon.IsCustom ? "New" : item.Icon.IsChanged ? "Edited" : "");

    private void AddIcon(EditableIcon? copyOf)
    {
        int value;
        try
        {
            value = Categories.NextFreeCustom(_items.Select(i => i.Icon.Value));
        }
        catch (InvalidOperationException e)
        {
            MessageBox.Show(this, e.Message, Title);
            return;
        }

        EditableIcon icon;
        if (copyOf != null)
        {
            icon = copyOf.Clone();
            icon.Value = value;
            icon.Name = DisplayName(copyOf) + " copy";
            if (icon.Colors.Count == 0)
            {
                icon.Colors.Add(FirstColor(copyOf));
            }

            // A new icon has no picture of its own; borrow the original's
            if (icon.Source == IconSource.Own)
            {
                icon.Source = IconSource.Game;
                icon.GameIcon = Categories.EnumName(copyOf.Value);
            }
        }
        else
        {
            var star = "icons/library/star.png";
            icon = new EditableIcon { Value = value, Name = "New icon", Colors = ["#FFFFFF"] };
            if (File.Exists(Path.Combine(_pluginFolder, star)))
            {
                icon.Source = IconSource.File;
                icon.File = star;
            }
            else
            {
                icon.Source = IconSource.Game;
                icon.GameIcon = "Default";
            }
        }

        var item = new IconItem(icon);
        Refresh(item);
        _items.Insert(_items.Count(i => i.Icon.IsCustom), item);
        IconList.SelectedItem = item;
        MarkDirty();
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void RemoveCurrent()
    {
        if (IconList.SelectedItem is not IconItem item)
        {
            return;
        }

        if (item.Icon.IsCustom)
        {
            var answer = MessageBox.Show(this, $"Remove \"{DisplayName(item.Icon)}\"?\n\nA character that has it goes back to the plain icon.", Title, MessageBoxButton.OKCancel);
            if (answer != MessageBoxResult.OK)
            {
                return;
            }

            var index = _items.IndexOf(item);
            _items.Remove(item);
            IconList.SelectedIndex = Math.Min(index, _items.Count - 1);
        }
        else
        {
            RememberForUndo(item);
            var icon = item.Icon;
            icon.Name = icon.GameIcon = icon.File = icon.Tint = null;
            icon.Colors.Clear();
            icon.Mode = NameMode.Solid;
            icon.Animate = 0;
            icon.Motion = Shared.Motions.Scroll;
            icon.Reverse = false;
            icon.Source = IconSource.Own;
            icon.Outline = icon.Glow = icon.Tag = icon.TagColor = icon.IconEffect = icon.Prestige = null;
            icon.Shadow = icon.Bold = icon.Italic = icon.SmallCaps = false;
            icon.Fps = 8;
            Refresh(item);
            ShowCurrent();
        }

        MarkDirty();
    }

    // ---------------------------------------------------------------- Editing

    private void ShowCurrent()
    {
        var icon = Current;
        if (icon == null)
        {
            return;
        }

        _loading = true;
        try
        {
            RemoveButton.Content = icon.IsCustom ? "Remove" : "Reset";
            RemoveButton.ToolTip = icon.IsCustom ? "Delete this icon" : "Put this game icon back the way the game has it";
            SlotText.Text = icon.IsCustom ? $"New icon · flag {icon.Value}" : "Game icon";
            NameBox.Tag = icon.IsCustom ? "Name your icon" : $"The game's name: {Categories.Label(icon.Value)}";
            NameBox.Text = icon.Name ?? "";

            var mode = icon.Colors.Count > 1 ? icon.Mode : NameMode.Solid;
            SolidMode.IsChecked = mode == NameMode.Solid;
            GradientMode.IsChecked = mode == NameMode.Gradient;
            LettersMode.IsChecked = mode == NameMode.Letters;
            // One color can still wave, sparkle, rainbow, glitch or bounce; scroll and pulse need two
            MotionScroll.IsEnabled = MotionPulse.IsEnabled = mode != NameMode.Solid;
            GameColorButton.Visibility = !icon.IsCustom && icon.Colors.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            AnimateSwitch.IsChecked = icon.Animate > 0;
            MotionScroll.IsChecked = icon.Motion == Shared.Motions.Scroll;
            MotionPulse.IsChecked = icon.Motion == Shared.Motions.Pulse;
            MotionWave.IsChecked = icon.Motion == Shared.Motions.Wave;
            MotionSparkle.IsChecked = icon.Motion == Shared.Motions.Sparkle;
            MotionRainbow.IsChecked = icon.Motion == Shared.Motions.Rainbow;
            MotionGlitch.IsChecked = icon.Motion == Shared.Motions.Glitch;
            MotionBounce.IsChecked = icon.Motion == Shared.Motions.Bounce;

            if (TagBox.Text != (icon.Tag ?? ""))
            {
                TagBox.Text = icon.Tag ?? "";
            }

            var ownTagColor = Images.TryParseColor(icon.TagColor, out var tagColor);
            TagColorRow.Visibility = icon.Tag != null ? Visibility.Visible : Visibility.Collapsed;
            TagGray.IsChecked = icon.TagColor == null;
            TagName.IsChecked = icon.TagColor == Shared.TagColors.Name;
            TagOwn.IsChecked = ownTagColor;
            TagColorChip.Visibility = ownTagColor ? Visibility.Visible : Visibility.Collapsed;
            TagColorChip.Background = new SolidColorBrush(tagColor);

            OutlineSwitch.IsChecked = icon.Outline != null;
            OutlineChip.Visibility = icon.Outline != null ? Visibility.Visible : Visibility.Collapsed;
            OutlineChip.Background = new SolidColorBrush(Images.ParseOr(icon.Outline, Colors.Black));
            GlowSwitch.IsChecked = icon.Glow != null;
            GlowChip.Visibility = icon.Glow != null ? Visibility.Visible : Visibility.Collapsed;
            GlowChip.Background = new SolidColorBrush(Images.ParseOr(icon.Glow, Colors.White));
            ShadowSwitch.IsChecked = icon.Shadow && icon.Glow == null;
            ItalicToggle.IsChecked = icon.Italic;

            EffectNone.IsChecked = icon.IconEffect == null;
            EffectPulse.IsChecked = icon.IconEffect == "pulse";
            EffectGlow.IsChecked = icon.IconEffect == "glow";
            EffectFlip.IsChecked = icon.IconEffect == "flip";
            EffectFloat.IsChecked = icon.IconEffect == "float";
            PrestigeGame.IsChecked = icon.Prestige == null;
            PrestigeHide.IsChecked = icon.Prestige == "hide";

            var frames = IconSheet(icon) is { } sheet ? Images.Frames(sheet).Count : 1;
            FpsPanel.Visibility = frames > 1 ? Visibility.Visible : Visibility.Collapsed;
            FpsCaption.Text = $"Animated · {frames} frames";
            FpsSlider.Value = icon.Fps;
            FpsText.Text = $"{icon.Fps:0} frames/s";
            ReverseSwitch.IsChecked = icon.Reverse;
            if (icon.Animate > 0)
            {
                SpeedSlider.Value = icon.Animate;
            }

            UpdateSpeedText();
            BuildChips();

            TintSwitch.IsChecked = icon.Tint != null;
            TintChip.Visibility = icon.Tint != null ? Visibility.Visible : Visibility.Collapsed;
            TintChip.Background = new SolidColorBrush(Images.ParseOr(icon.Tint, Colors.White));
            OwnIconButton.Visibility = !icon.IsCustom && icon.Source != IconSource.Own ? Visibility.Visible : Visibility.Collapsed;
            IconSourceText.Text = icon.Source switch
            {
                IconSource.Own => "The game's own icon",
                IconSource.Game => $"The game's {Categories.Label(Categories.Parse(icon.GameIcon ?? "Default") ?? 0)} icon",
                _ => Path.GetFileNameWithoutExtension(icon.File ?? ""),
            };
        }
        finally
        {
            _loading = false;
        }

        UpdatePreview();
    }

    /// <summary>
    /// Applies a change to the selected icon and shows it everywhere. <paramref name="undoGroup"/> names a
    /// control that changes continuously (a slider, a text box, a color being picked): its changes in quick
    /// succession are one undo step. Without one, every change is a step of its own.
    /// </summary>
    private void Edit(Action<EditableIcon> change, bool reshow = false, string? undoGroup = null)
    {
        if (_loading || IconList.SelectedItem is not IconItem item)
        {
            return;
        }

        RememberForUndo(item, undoGroup);
        change(item.Icon);
        Refresh(item);
        MarkDirty();
        if (reshow)
        {
            ShowCurrent();
        }
        else
        {
            UpdatePreview();
        }
    }

    // ---------------------------------------------------------------- Undo

    // Icons as they were before each change, newest last. Changes from one continuous control to one
    // icon within a moment of each other (a slider dragged, a name typed) are one step.
    private readonly List<(IconItem Item, EditableIcon Before)> _undo = [];
    private IconItem? _undoItem;
    private string? _undoGroup;
    private DateTime _undoAt;
    private const int UndoSteps = 100;

    private void RememberForUndo(IconItem item, string? group = null)
    {
        var now = DateTime.UtcNow;
        if (group != null && group == _undoGroup && item == _undoItem && now - _undoAt < TimeSpan.FromMilliseconds(900))
        {
            _undoAt = now;
            return;
        }

        _undo.Add((item, item.Icon.Clone()));
        if (_undo.Count > UndoSteps)
        {
            _undo.RemoveAt(0);
        }

        _undoItem = item;
        _undoGroup = group;
        _undoAt = now;
    }

    private void Undo()
    {
        // Steps for icons removed since are skipped
        while (_undo.Count > 0)
        {
            var (item, before) = _undo[^1];
            _undo.RemoveAt(_undo.Count - 1);
            if (!_items.Contains(item))
            {
                continue;
            }

            item.Icon = before;
            _undoItem = null;
            _undoGroup = null;
            Refresh(item);
            if (IconList.SelectedItem == item)
            {
                ShowCurrent();
            }
            else
            {
                IconList.SelectedItem = item;
            }

            MarkDirty();
            SetStatus(_undo.Count > 0 ? "Undone. Ctrl+Z again to go further back." : "Undone. Nothing older to undo.");
            return;
        }

        SetStatus("Nothing to undo");
    }

    private void SetMode(NameMode mode)
    {
        Edit(i =>
        {
            if (mode != NameMode.Solid)
            {
                // A gradient needs two colors to go between
                if (i.Colors.Count == 0)
                {
                    i.Colors.Add(FirstColor(i));
                }

                if (i.Colors.Count == 1)
                {
                    i.Colors.Add(Images.ToHex(ShiftHue(Images.ParseOr(i.Colors[0], Colors.White), 70)));
                }
            }

            i.Mode = mode;
        }, reshow: true);
    }

    private void BuildChips()
    {
        ColorChips.Children.Clear();
        var icon = Current;
        if (icon == null)
        {
            return;
        }

        var solid = icon.Colors.Count <= 1 || icon.Mode == NameMode.Solid;
        var shown = icon.Colors.Count == 0 ? [FirstColor(icon)] : solid ? [icon.Colors[0]] : icon.Colors.ToList();

        for (var index = 0; index < shown.Count; index++)
        {
            var i = index;
            var chip = new Button
            {
                Width = 58, Height = 36, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(0),
                Background = new SolidColorBrush(Images.ParseOr(shown[i], Colors.White)), ToolTip = shown[i],
            };
            chip.Click += (_, _) => ColorPicker.Open(chip, Images.ParseOr(shown[i], Colors.White), c =>
            {
                chip.Background = new SolidColorBrush(c);
                chip.ToolTip = Images.ToHex(c);
                Edit(ic =>
                {
                    if (ic.Colors.Count == 0)
                    {
                        ic.Colors.Add(Images.ToHex(c));
                    }
                    else
                    {
                        ic.Colors[i] = Images.ToHex(c);
                    }
                }, undoGroup: $"color{i}");
                GameColorButton.Visibility = !icon.IsCustom ? Visibility.Visible : Visibility.Collapsed;
            });

            if (!solid)
            {
                var menu = new ContextMenu();
                AddMenu(menu, "Move left", i > 0, () => Edit(ic => (ic.Colors[i - 1], ic.Colors[i]) = (ic.Colors[i], ic.Colors[i - 1]), reshow: true));
                AddMenu(menu, "Move right", i < shown.Count - 1, () => Edit(ic => (ic.Colors[i + 1], ic.Colors[i]) = (ic.Colors[i], ic.Colors[i + 1]), reshow: true));
                AddMenu(menu, "Remove", shown.Count > 2, () => Edit(ic => ic.Colors.RemoveAt(i), reshow: true));
                chip.ContextMenu = menu;
            }

            ColorChips.Children.Add(chip);
        }

        if (!solid)
        {
            var add = new Button { Content = "+", Width = 58, Height = 36, Margin = new Thickness(0, 0, 8, 8), FontSize = 18, Padding = new Thickness(0), ToolTip = "Add a color" };
            add.Click += (_, _) => Edit(ic => ic.Colors.Add(Images.ToHex(ShiftHue(Images.ParseOr(ic.Colors[^1], Colors.White), 45))), reshow: true);
            ColorChips.Children.Add(add);
        }

        ColorChips.ToolTip = solid
            ? "Click the color to change it. Pick Gradient or Per letter for more colors."
            : "Click a color to change it, right-click to move or remove it.";
    }

    private static void AddMenu(ContextMenu menu, string header, bool enabled, Action action)
    {
        var item = new MenuItem { Header = header, IsEnabled = enabled };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private void UpdateSpeedText()
    {
        var letters = LettersMode.IsChecked == true;
        SpeedText.Text = letters ? $"{SpeedSlider.Value * 4:0.0} steps/s" : $"{SpeedSlider.Value:0.00} loops/s";
    }

    // ---------------------------------------------------------------- How an icon looks

    private string DisplayName(EditableIcon icon) => icon.Name ?? Categories.Label(icon.Value);

    private string FirstColor(EditableIcon icon) =>
        icon.Colors.Count > 0 ? icon.Colors[0] : _game.Member(icon.Value)?.Color ?? (icon.IsCustom ? "#FFFFFF" : StandardGray);

    private NameLook Look(EditableIcon icon)
    {
        var colors = icon.Colors.Count > 0 ? icon.Colors : [FirstColor(icon)];
        var parsed = colors.Select(c => Images.ParseOr(c, Colors.White)).ToList();
        var fx = new NameFx(
            icon.Outline != null ? Images.ParseOr(icon.Outline, Colors.Black) : null,
            icon.Glow != null ? Images.ParseOr(icon.Glow, Colors.White) : null,
            icon.Shadow && icon.Glow == null, icon.Bold, icon.Italic, icon.SmallCaps);
        var mode = parsed.Count > 1 ? icon.Mode : NameMode.Solid;
        return new NameLook(parsed, mode, icon.Animate, icon.Motion, icon.Reverse, fx.Any ? fx : null, icon.Tag, icon.TagColor);
    }

    // Letters and digits only, as a tag reads in game; empty for none
    private static string? CleanTag(string text)
    {
        var tag = new string(text.Where(char.IsLetterOrDigit).Take(Shared.BotTags.MaxLength).ToArray()).ToUpperInvariant();
        return tag.Length == 0 ? null : tag;
    }

    private void PickEffectColor(Button chip, Func<EditableIcon, string?> get, Action<EditableIcon, string> set)
    {
        if (Current is { } icon)
        {
            ColorPicker.Open(chip, Images.ParseOr(get(icon), Colors.White), c =>
            {
                chip.Background = new SolidColorBrush(c);
                Edit(i => set(i, Images.ToHex(c)), undoGroup: chip.Name);
            });
        }
    }

    private bool _settingOptions;

    // A setting of the whole file (your scav, sharing, nemesis) rather than of one icon
    private void SetFileOption(Action<IconsFile> change)
    {
        if (_settingOptions)
        {
            return;
        }

        change(_file);
        MarkDirty();
    }

    private string? IconFile(EditableIcon icon) => icon.Source switch
    {
        IconSource.Own => _game.MemberIconFile(icon.Value),
        IconSource.Game => _game.MemberIconFile(Categories.Parse(icon.GameIcon ?? "Default") ?? 0),
        _ => icon.File,
    };

    /// <summary>The picture the game will show, recolor included; all its frames if it's animated.</summary>
    private BitmapSource? IconSheet(EditableIcon icon)
    {
        var file = IconFile(icon);
        var image = file == null ? null : Images.Load(Path.Combine(_pluginFolder, file));
        return image != null && Images.TryParseColor(icon.Tint, out var tint) ? Images.Tint(image, tint) : image;
    }

    /// <summary>The picture as a still: an animated icon's first frame.</summary>
    private BitmapSource? IconImage(EditableIcon icon) => IconSheet(icon) is { } sheet ? Images.FirstFrame(sheet) : null;

    private void UpdatePreview()
    {
        if (Current is not { } icon)
        {
            return;
        }

        var image = IconImage(icon);
        var look = Look(icon);
        foreach (var target in new[] { PreviewIcon, BigIcon })
        {
            target.Source = image;

            // The game's member icons are ~20 px; blow those up without blurring
            RenderOptions.SetBitmapScalingMode(target, image is { PixelWidth: < 48 } ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        }

        StartIconPreview(icon);
        PreviewName.Text = _nickname;
        PreviewName.Look = look;

        // The dropdown draws built-in icons from a fixed sprite sheet: always the game's picture
        DropdownIcon.Source = icon.IsCustom ? null : Images.Load(Path.Combine(_pluginFolder, _game.MemberIconFile(icon.Value) ?? "-"));
        DropdownIcon.Visibility = icon.IsCustom ? Visibility.Collapsed : Visibility.Visible;
        DropdownName.Text = DisplayName(icon);
        DropdownName.Look = look with { Speed = 0, Tag = null };
        DropdownBox.ToolTip = icon.IsCustom
            ? "New icons show without a picture in this dropdown; it only has the game's."
            : icon.Source != IconSource.Own || icon.Tint != null
                ? "This dropdown keeps the game's own small picture."
                : null;
    }

    // ---------------------------------------------------------------- Gallery

    private void ShowGallery()
    {
        if (!IsLoaded)
        {
            return;
        }

        _thumbLoad?.Cancel();
        _gallery.Clear();

        // Every tab: the library alone is a couple of hundred icons. "animated-" isn't part of a name.
        var search = SearchBox.Text.Trim();
        bool Match(string name) => search.Length == 0 || name.Replace("animated-", "").Replace('-', ' ').Contains(search, StringComparison.OrdinalIgnoreCase)
                                   || name.Contains(search, StringComparison.OrdinalIgnoreCase);

        GameBar.Visibility = GameTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        var items = new List<GalleryItem>();

        if (LibraryTab.IsChecked == true)
        {
            items.AddRange(Categories.WithIcons.Where(v => _game.MemberIconFile(v) != null && Match(Categories.Label(v)))
                .Select(v => new GalleryItem(Categories.Label(v), _game.MemberIconFile(v)!, v)));
            items.AddRange(Pngs("icons/library").Where(p => !IsAnimated(p) && Match(Path.GetFileNameWithoutExtension(p)))
                .Select(p => new GalleryItem(Path.GetFileNameWithoutExtension(p), p, null)));
        }
        else if (AnimatedTab.IsChecked == true)
        {
            items.AddRange(Pngs("icons/library").Concat(Pngs("icons")).Concat(Pngs("icons/imported")).Where(p => Match(Path.GetFileNameWithoutExtension(p)) && IsAnimated(p))
                .Select(p => new GalleryItem(Path.GetFileNameWithoutExtension(p), p, null)));
        }
        else if (GameTab.IsChecked == true)
        {
            var large = LargeSwitch.IsChecked == true;
            var sprites = _game.Sprites.Where(s => (large || Math.Max(s.Width, s.Height) <= 128) && Match(s.Name)).ToList();
            GameCountText.Text = _game.Extracted
                ? $"{sprites.Count} of {_game.Sprites.Count} images from the game{(large ? "" : ", icon-sized ones")}"
                : "The game's icons haven't been read yet.";
            items.AddRange(sprites.Select(s => new GalleryItem(s.Name, "game-icons/" + s.File, null)));
        }
        else
        {
            items.AddRange(Pngs("icons").Concat(Pngs("icons/imported")).Where(p => !IsAnimated(p) && Match(Path.GetFileNameWithoutExtension(p)))
                .Select(p => new GalleryItem(Path.GetFileNameWithoutExtension(p), p, null)));
        }

        foreach (var item in items)
        {
            _gallery.Add(item);
        }

        _loading = true;
        Gallery.SelectedItem = Current is { } icon ? _gallery.FirstOrDefault(g => Matches(g, icon)) : null;
        _loading = false;

        // Thumbnails load in the background so a big list opens at once
        var cancel = _thumbLoad = new CancellationTokenSource();
        var folder = _pluginFolder;
        _ = Task.Run(() =>
        {
            foreach (var item in items)
            {
                if (cancel.IsCancellationRequested)
                {
                    return;
                }

                var frames = ThumbnailFrames(Path.Combine(folder, item.Path));
                Dispatcher.InvokeAsync(() =>
                {
                    item.SetFrames(frames);
                    if (item.Animated)
                    {
                        EnsureFrameLoop();
                    }
                }, DispatcherPriority.Background);
            }
        });
    }

    private bool IsAnimated(string relativePath) => Images.IsStrip(Path.Combine(_pluginFolder, relativePath));

    private bool Matches(GalleryItem item, EditableIcon icon) => item.Member is { } member
        ? (icon.Source == IconSource.Own && member == icon.Value) || (icon.Source == IconSource.Game && Categories.Parse(icon.GameIcon ?? "") == member)
        : icon.Source == IconSource.File && string.Equals(item.Path, icon.File, StringComparison.OrdinalIgnoreCase);

    private IEnumerable<string> Pngs(string relativeFolder)
    {
        var folder = Path.Combine(_pluginFolder, relativeFolder);
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(f => relativeFolder + "/" + Path.GetFileName(f))
            : [];
    }

    /// <summary>A thumbnail's frames: one for a still picture, all of them for an animated one.</summary>
    private static IReadOnlyList<BitmapSource> ThumbnailFrames(string path) =>
        Thumbnail(path, all: true) is { } sheet ? Images.Frames(sheet) : [];

    private static BitmapSource? Thumbnail(string path, bool all = false)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(File.ReadAllBytes(path));
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            // An animated icon shows its first frame, unless all of them are wanted
            image.Freeze();
            return all ? image : Images.FirstFrame(image);
        }
        catch (Exception e) when (e is IOException or NotSupportedException or FileFormatException or ArgumentException
                                   or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            return null;
        }
    }

    private void PickFromGallery()
    {
        if (_loading || Gallery.SelectedItem is not GalleryItem item || Current is not { } icon || Matches(item, icon))
        {
            return;
        }

        Edit(i =>
        {
            if (item.Member is { } member)
            {
                if (!i.IsCustom && member == i.Value)
                {
                    i.Source = IconSource.Own;
                    i.GameIcon = null;
                }
                else
                {
                    i.Source = IconSource.Game;
                    i.GameIcon = Categories.EnumName(member);
                }
            }
            else
            {
                i.Source = IconSource.File;
                i.File = item.Path;
            }
        }, reshow: true);
    }

    // ---------------------------------------------------------------- Saving

    private bool Save()
    {
        var unnamed = _items.FirstOrDefault(i => i.Icon.IsCustom && string.IsNullOrWhiteSpace(i.Icon.Name));
        if (unnamed != null)
        {
            IconList.SelectedItem = unnamed;
            MessageBox.Show(this, "A new icon needs a name.", Title);
            return false;
        }

        var file = new IconsFile
        {
            DumpOriginalIcons = _file.DumpOriginalIcons, ScavLook = _file.ScavLook, ShareNamePlate = _file.ShareNamePlate, Nemesis = _file.Nemesis, Presets = _file.Presets,
        };
        try
        {
            foreach (var icon in _items.Select(i => i.Icon).Where(i => i.IsChanged))
            {
                string? baked = null;
                if (icon.Tint != null)
                {
                    // The plugin takes a finished PNG, so a recolor is saved as one
                    var image = IconSheet(icon) ?? throw new InvalidOperationException($"\"{DisplayName(icon)}\" is recolored, but its picture couldn't be loaded.");
                    baked = $"icons/generated/{icon.Key}.png";
                    Directory.CreateDirectory(Path.Combine(_pluginFolder, "icons", "generated"));
                    Images.SavePng(image, Path.Combine(_pluginFolder, baked));
                }

                file.Icons.Add(IconsFile.FromEditable(icon, baked));
            }

            file.Icons.AddRange(_kept);
            file.Bots = _bots;
            Directory.CreateDirectory(_pluginFolder);
            BackUpIconsJson();
            file.Save(_iconsJson);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, $"Not saved.\n\n{e.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        _file = file;
        _dirty = false;
        SaveButton.IsEnabled = false;
        SetStatus($"Saved {DateTime.Now:HH:mm}. A running game picks it up when you reopen a screen.");
        return true;
    }

    // The icons.json this window opened, copied before the first save replaces it
    private void BackUpIconsJson()
    {
        if (_backedUp || !File.Exists(_iconsJson))
        {
            return;
        }

        var folder = Path.Combine(_pluginFolder, "backups");
        Directory.CreateDirectory(folder);
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backup = Path.Combine(folder, $"icons.json.{stamp}");
        for (var n = 2; File.Exists(backup); n++)
        {
            backup = Path.Combine(folder, $"icons.json.{stamp}_{n}");
        }

        File.Copy(_iconsJson, backup);
        _backedUp = true;
    }

    private void MarkDirty()
    {
        _dirty = true;
        SaveButton.IsEnabled = true;
        SetStatus("Unsaved changes");
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_dirty)
        {
            return;
        }

        var answer = MessageBox.Show(this, "Save your icon changes?", Title, MessageBoxButton.YesNoCancel);
        e.Cancel = answer == MessageBoxResult.Cancel || (answer == MessageBoxResult.Yes && !Save());
    }

    // ---------------------------------------------------------------- Profile page

    private void ShowPage()
    {
        if (!IsLoaded)
        {
            return;
        }

        var profile = ProfileTab.IsChecked == true;
        var bots = BotsTab.IsChecked == true;
        IconsPage.Visibility = !profile && !bots ? Visibility.Visible : Visibility.Collapsed;
        BotsPage.Visibility = bots ? Visibility.Visible : Visibility.Collapsed;
        ProfilePage.Visibility = profile ? Visibility.Visible : Visibility.Collapsed;

        // The page shown may have things to play; the loop stops itself if not
        EnsureFrameLoop();
        if (bots)
        {
            RefreshBotPreview();
            ShowNemesis();
        }

        if (profile)
        {
            LoadProfiles();
            UpdateServerState();
            _serverTimer.Start();
        }
        else
        {
            _serverTimer.Stop();
        }
    }

    private void LoadProfiles()
    {
        var current = (ProfileList.SelectedItem as ProfileItem)?.Summary.Path;
        _profileItems.Clear();
        foreach (var profile in _profiles.List())
        {
            var shown = _items.FirstOrDefault(i => i.Icon.Value == profile.Selected)?.Icon;
            _profileItems.Add(new ProfileItem(
                profile,
                profile.Nickname ?? profile.Username,
                profile.HasCharacter ? $"{profile.Username} · {profile.Edition}" : $"{profile.Username} · no character yet",
                shown != null ? Look(shown) : NameLook.Solid(Images.ParseOr(StandardGray, Colors.Gray)),
                shown != null ? IconImage(shown) : null));
        }

        ProfileList.SelectedItem = _profileItems.FirstOrDefault(p => p.Summary.Path == current) ?? _profileItems.FirstOrDefault(p => p.Summary.HasCharacter);
        if (_profileItems.Count == 0)
        {
            ProfileNote.Text = $"No profiles in {_profiles.Folder}. Playing on a Fika host? Your icon is chosen there (Name Plate.exe "
                + "on the host, or the 'spt membercategory' chat command). A Name Plate made there reaches you; one made here wins.";
        }
    }

    private void ShowProfile()
    {
        OwnedPanel.Children.Clear();
        ShownPanel.Children.Clear();
        if (ProfileList.SelectedItem is not ProfileItem { Summary: var profile })
        {
            return;
        }

        var choices = _items.Where(i => i.Icon.IsCustom).Select(i => i.Icon.Value)
            .Concat(Categories.Grantable)
            .ToList();

        // Custom flags on the profile that the icon list doesn't have any more
        for (var bit = Categories.FirstCustom; bit > 0; bit <<= 1)
        {
            if ((profile.MemberCategory & bit) != 0 && !choices.Contains(bit))
            {
                choices.Add(bit);
            }
        }

        foreach (var value in choices)
        {
            var tile = new ToggleButton { Style = (Style)FindResource("ChoiceTile"), Tag = value, Content = ChoiceContent(value), IsChecked = (profile.MemberCategory & value) == value };
            tile.Checked += (_, _) => BuildShown(profile.Selected);
            tile.Unchecked += (_, _) => BuildShown(profile.Selected);
            OwnedPanel.Children.Add(tile);
        }

        BuildShown(profile.Selected);
        ApplyProfileButton.IsEnabled = profile.HasCharacter && !Profiles.ServerRunning();
        ProfileNote.Text = profile.HasCharacter ? "" : "This profile has no character yet.";
    }

    private void BuildShown(int selected)
    {
        var previous = ShownPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Tag as int? ?? selected;
        ShownPanel.Children.Clear();
        var options = new List<int> { 0 };
        options.AddRange(OwnedPanel.Children.OfType<ToggleButton>().Where(t => t.IsChecked == true).Select(t => (int)t.Tag));
        if (!options.Contains(previous))
        {
            previous = options[^1];
        }

        foreach (var value in options)
        {
            ShownPanel.Children.Add(new RadioButton
            {
                Style = (Style)FindResource("ChoiceTile"), GroupName = "Shown", Tag = value, Content = ChoiceContent(value), IsChecked = value == previous,
            });
        }
    }

    private FrameworkElement ChoiceContent(int value)
    {
        var icon = _items.FirstOrDefault(i => i.Icon.Value == value)?.Icon;
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var image = icon != null ? IconImage(icon) : Images.Load(Path.Combine(_pluginFolder, _game.MemberIconFile(value) ?? "-"));
        panel.Children.Add(new Image { Source = image, Width = 22, Height = 22, StretchDirection = StretchDirection.DownOnly, Margin = new Thickness(0, 0, 10, 0) });
        panel.Children.Add(new StyledName
        {
            Text = icon != null ? DisplayName(icon) : value == 0 ? "Standard (no icon)" : Categories.IsBuiltin(value) ? Categories.Label(value) : $"Custom {value} (not in your icons)",
            Look = icon != null ? Look(icon) with { Tag = null } : NameLook.Solid(Images.ParseOr(StandardGray, Colors.Gray)),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return panel;
    }

    private void ApplyProfile()
    {
        if (ProfileList.SelectedItem is not ProfileItem { Summary: var profile })
        {
            return;
        }

        // Only the flags listed here change; anything else on the profile stays
        var tiles = OwnedPanel.Children.OfType<ToggleButton>().ToList();
        var listed = tiles.Aggregate(0, (mask, t) => mask | (int)t.Tag);
        var owned = tiles.Where(t => t.IsChecked == true).Aggregate(0, (mask, t) => mask | (int)t.Tag);
        var memberCategory = (profile.MemberCategory & ~listed) | owned;
        var selected = ShownPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Tag as int? ?? 0;

        try
        {
            var backup = _profiles.Write(profile, memberCategory, selected);
            LoadProfiles();
            SetStatus("Saved to the character");
            ProfileNote.Text = $"Saved. Start the server, then the game. The old profile is backed up in {Path.GetDirectoryName(backup)}"
                + (_dirty ? "\nYour icon changes aren't saved yet: press Save too." : "")
                + "\nPlaying from another PC (Fika)? This Name Plate reaches them through the server, unless they made their own for this icon.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException or System.Text.Json.JsonException)
        {
            MessageBox.Show(this, $"Not saved.\n\n{e.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateServerState()
    {
        var running = Profiles.ServerRunning();
        var color = (Color)FindResource(running ? "Danger" : "Good");
        ServerDot.Fill = new SolidColorBrush(color);
        ServerPill.BorderBrush = new SolidColorBrush(Color.FromArgb(120, color.R, color.G, color.B));
        ServerPill.Background = new SolidColorBrush(Color.FromArgb(28, color.R, color.G, color.B));
        ServerText.Text = running
            ? "The SPT server is running. Close it before saving: it would save over the change."
            : "The SPT server is closed. Safe to save.";
        ApplyProfileButton.IsEnabled = !running && ProfileList.SelectedItem is ProfileItem { Summary.HasCharacter: true };
    }

    // ---------------------------------------------------------------- Helpers

    private static Color ShiftHue(Color color, double degrees)
    {
        ColorPicker.ToHsv(color, out var h, out var s, out var v);
        return ColorPicker.FromHsv((h + degrees) % 360, Math.Max(s, 0.55), Math.Max(v, 0.75));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void DarkTitleBar()
    {
        var on = 1;
        _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref on, sizeof(int));
    }
}

public abstract class Notifying : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Changed(string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class IconItem(EditableIcon icon) : Notifying
{
    /// <summary>Replaced as a whole by Undo.</summary>
    public EditableIcon Icon { get; set; } = icon;
    public string DisplayName { get; private set; } = "";
    public NameLook Look { get; private set; } = NameLook.Solid(Colors.White);
    public ImageSource? Thumbnail { get; private set; }
    public string Tag { get; private set; } = "";

    public void Update(string name, NameLook look, ImageSource? thumbnail, string tag)
    {
        DisplayName = name;
        Look = look;
        Thumbnail = thumbnail;
        Tag = tag;
        Changed();
    }
}

public class GalleryItem(string name, string path, int? member) : Notifying
{
    private ImageSource? _thumbnail;
    private IReadOnlyList<BitmapSource> _frames = [];
    private int _frame;

    /// <summary>An animated icon's frames; the thumbnail plays through them (see ShowFrame).</summary>
    public bool Animated => _frames.Count > 1;

    public void SetFrames(IReadOnlyList<BitmapSource> frames)
    {
        _frames = frames;
        _frame = 0;
        Thumbnail = frames.Count > 0 ? frames[0] : null;
    }

    /// <summary>Shows the frame for a moment in time, if it isn't showing already.</summary>
    public void ShowFrame(double seconds, double fps)
    {
        var frame = (int)(seconds * fps) % _frames.Count;
        if (frame != _frame)
        {
            _frame = frame;
            Thumbnail = _frames[frame];
        }
    }

    public string Name { get; } = name;

    /// <summary>Relative to the plugin folder.</summary>
    public string Path { get; } = path;

    /// <summary>Set for the game's member icons, which the plugin borrows at runtime.</summary>
    public int? Member { get; } = member;

    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set
        {
            _thumbnail = value;
            Changed(nameof(Thumbnail));
        }
    }
}

public record ProfileItem(Profiles.Summary Summary, string Title, string Subtitle, NameLook Look, ImageSource? Thumbnail);
