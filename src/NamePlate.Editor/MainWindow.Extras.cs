using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NamePlate.Editor;

/// <summary>
/// Presets (the built-in ones and your own), share codes, and the preview of an animated icon and
/// its effect.
/// </summary>
public partial class MainWindow
{
    // ---------------------------------------------------------------- Presets

    /// <summary>The built-in presets, then yours (right-click to remove), then a button to add one.</summary>
    private void BuildPresets()
    {
        PresetPanel.Children.Clear();
        foreach (var (name, colors) in Presets)
        {
            PresetPanel.Children.Add(PresetButton(name, colors, null));
        }

        foreach (var preset in _file.Presets ?? [])
        {
            if (preset.Colors.Count >= 2)
            {
                PresetPanel.Children.Add(PresetButton(preset.Name, [.. preset.Colors], preset));
            }
        }

        var save = new Button { Content = "+ Save these colors", Style = (Style)FindResource("GhostBtn"), Margin = new Thickness(0, 0, 8, 8), ToolTip = "Keep the current colors as a preset of your own" };
        save.Click += (_, _) => SaveCurrentAsPreset();
        PresetPanel.Children.Add(save);
    }

    private Button PresetButton(string name, string[] colors, IconsFile.UserPreset? own)
    {
        var look = new NameLook(colors.Select(c => Images.ParseOr(c, Colors.White)).ToList(), NameMode.Gradient, 0);
        var button = new Button
        {
            Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(14, 0, 14, 0),
            Content = new StyledName { Text = name, Look = look, FontSize = 13, FontWeight = FontWeights.SemiBold },
        };
        button.Click += (_, _) => Edit(i =>
        {
            PresetsButton.IsChecked = false;
            i.Colors = [.. colors];
            if (i.Mode == NameMode.Solid)
            {
                i.Mode = NameMode.Gradient;
            }
        }, reshow: true);

        if (own != null)
        {
            button.ToolTip = "Your preset. Right-click to remove it.";
            var menu = new ContextMenu();
            AddMenu(menu, "Remove", true, () =>
            {
                _file.Presets?.Remove(own);
                MarkDirty();
                BuildPresets();
            });
            button.ContextMenu = menu;
        }

        return button;
    }

    private void SaveCurrentAsPreset()
    {
        if (Current is not { } icon || icon.Colors.Count < 2 || icon.Mode == NameMode.Solid)
        {
            SetStatus("Pick Gradient or Per letter with two or more colors first");
            return;
        }

        _file.Presets ??= [];
        _file.Presets.Add(new IconsFile.UserPreset { Name = $"Mine {_file.Presets.Count + 1}", Colors = [.. icon.Colors] });
        MarkDirty();
        BuildPresets();
        SetStatus("Saved as a preset. Press Save to keep it.");
    }

    // ---------------------------------------------------------------- Surprise me

    /// <summary>
    /// A random look on the current icon, made by the generator bots get theirs from, with every
    /// icon and preset in play and the extras a little likelier. The icon's name stays.
    /// </summary>
    private void Surprise()
    {
        if (Current is null)
        {
            return;
        }

        var rules = DefaultBots().ToRules();
        rules.AnimateChance = 60;
        rules.TagChance = 50;
        rules.TagColorChance = 50;
        rules.EffectChance = 40;
        rules.IconEffectChance = 30;

        // Bold and small caps show nothing on your name (already bold, in capitals)
        rules.Effects = [Shared.NameEffectKinds.Outline, Shared.NameEffectKinds.Glow, Shared.NameEffectKinds.Shadow, Shared.NameEffectKinds.Italic];
        var look = Shared.BotLookGenerator.For(Guid.NewGuid().ToString("N"), rules);
        if (look == null)
        {
            return;
        }

        Edit(i =>
        {
            i.Colors = [.. look.Colors];
            i.Mode = look.Mode switch { "gradient" => NameMode.Gradient, "letters" => NameMode.Letters, _ => NameMode.Solid };
            i.Animate = look.Motion != null ? look.Speed : 0;
            i.Motion = look.Motion ?? Shared.Motions.Scroll;
            i.Reverse = look.Reverse;
            i.Outline = look.Effect == Shared.NameEffectKinds.Outline ? look.EffectColor : null;
            i.Glow = look.Effect == Shared.NameEffectKinds.Glow ? look.EffectColor : null;
            i.Shadow = look.Effect == Shared.NameEffectKinds.Shadow;
            i.Italic = look.Effect == Shared.NameEffectKinds.Italic;
            i.Tag = look.Tag;
            i.TagColor = look.TagColor;
            i.IconEffect = look.IconEffect;
            i.Tint = null;

            if (look.Icon is { } picture && picture.StartsWith(Shared.BotLookGenerator.MemberPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var member = Categories.Parse(picture[Shared.BotLookGenerator.MemberPrefix.Length..]) ?? 0;
                i.Source = !i.IsCustom && member == i.Value ? IconSource.Own : IconSource.Game;
                i.GameIcon = i.Source == IconSource.Game ? Categories.EnumName(member) : null;
                i.File = null;
            }
            else if (look.Icon != null)
            {
                i.Source = IconSource.File;
                i.File = look.Icon;
                i.GameIcon = null;
            }
        }, reshow: true);

        SetStatus("A random look. Surprise me again for another, Ctrl+Z for the one before, Save to keep it.");
    }

    // ---------------------------------------------------------------- Share codes

    /// <summary>
    /// The current look as a code on the clipboard: colors, motion, effects, tag, and the picture
    /// when it's one everybody has (the library or a game icon).
    /// </summary>
    private void CopyCode()
    {
        if (Current is not { } icon)
        {
            return;
        }

        var entry = IconsFile.FromEditable(icon, null);
        entry.Category = "";
        entry.Name = null;
        var shareable = icon.Source == IconSource.Game
            || (icon.Source == IconSource.File && icon.File != null && icon.File.Replace('\\', '/').StartsWith("icons/library/", StringComparison.OrdinalIgnoreCase));
        if (!shareable)
        {
            entry.Icon = entry.IconFrom = null;
            entry.Editor = null;
        }

        try
        {
            Clipboard.SetText(IconsFile.ToCode(entry));
            SetStatus(shareable ? "Look copied as a code" : "Look copied as a code (without the picture: it's only on this PC)");
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            SetStatus("Couldn't use the clipboard; try again");
        }
    }

    /// <summary>A shared code's look on the current icon. Its name stays; the picture changes only if it came along.</summary>
    private void PasteCode()
    {
        if (Current is null)
        {
            return;
        }

        string text;
        try
        {
            text = Clipboard.GetText();
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            SetStatus("Couldn't use the clipboard; try again");
            return;
        }

        var entry = IconsFile.FromCode(text);
        if (entry == null)
        {
            SetStatus("The clipboard doesn't hold a look code (they start with CI1-)");
            return;
        }

        Edit(i =>
        {
            entry.Category = i.Key;
            var look = IconsFile.ToEditable(entry);
            i.Colors = look.Colors;
            i.Mode = look.Mode;
            i.Animate = look.Animate;
            i.Motion = look.Motion;
            i.Reverse = look.Reverse;
            i.Outline = look.Outline;
            i.Glow = look.Glow;
            i.Shadow = look.Shadow;
            i.Bold = look.Bold;
            i.Italic = look.Italic;
            i.SmallCaps = look.SmallCaps;
            i.Tag = look.Tag;
            i.TagColor = look.TagColor;
            i.IconEffect = look.IconEffect;
            i.Prestige = look.Prestige;

            var hasPicture = entry.Icon != null || entry.IconFrom != null;
            var fileThere = look.Source != IconSource.File || (look.File != null && File.Exists(Path.Combine(_pluginFolder, look.File)));
            if (hasPicture && fileThere && !(i.IsCustom && look.Source == IconSource.Own))
            {
                i.Source = look.Source;
                i.GameIcon = look.GameIcon;
                i.File = look.File;
                i.Tint = look.Tint;
                i.Fps = look.Fps;
            }
        }, reshow: true);
        SetStatus("Look pasted");
    }

    // ---------------------------------------------------------------- Animated icon preview

    private Image[] PreviewImages => [PreviewIcon, BigIcon];

    // Made once when the preview starts, then only their values change frame to frame: building a
    // new blur 30 times a second for three images is what made the preview stutter
    private readonly List<(Image Image, ScaleTransform Scale, TranslateTransform Move, DropShadowEffect? Glow)> _previewParts = [];
    private int _previewFrame = -1;

    /// <summary>Plays the current icon's frames and effect in the preview, or stops if there's nothing to play.</summary>
    private void StartIconPreview(EditableIcon icon)
    {
        _iconPlaying = icon;
        _iconFrames = IconSheet(icon) is { } sheet ? Images.Frames(sheet) : [];
        _previewFrame = -1;

        // An animated icon moves by itself: turning it over on top (a coin spinning two ways) looks broken
        EffectFlip.IsEnabled = _iconFrames.Count <= 1;
        EffectFlip.ToolTip = EffectFlip.IsEnabled ? "Turns like a coin" : "Not for animated icons: they already move";
        _previewParts.Clear();

        var tint = Images.ParseOr(icon.Colors.Count > 0 ? icon.Colors[0] : FirstColor(icon), Colors.White);
        foreach (var image in PreviewImages)
        {
            var scale = new ScaleTransform();
            var move = new TranslateTransform();
            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(move);
            image.RenderTransformOrigin = new Point(0.5, 0.5);
            image.RenderTransform = group;

            DropShadowEffect? glow = null;
            if (icon.IconEffect == "glow")
            {
                glow = new DropShadowEffect { Color = tint, ShadowDepth = 0, BlurRadius = Math.Max(8, image.Width * 0.6) };
            }

            image.Effect = glow;
            _previewParts.Add((image, scale, move, glow));
        }

        _iconAnimating = _iconFrames.Count > 1 || icon.IconEffect != null;
        if (_iconAnimating)
        {
            EnsureFrameLoop();
            PlayIcon();
        }
    }

    // One frame of the preview, as IconAnimator runs it in game
    private void PlayIcon()
    {
        if (_iconPlaying is not { } icon || !IsVisible)
        {
            return;
        }

        var t = (DateTime.UtcNow - IconClock).TotalSeconds;
        BitmapSource? frame = null;
        if (_iconFrames.Count > 1)
        {
            var index = (int)(t * Math.Max(1, icon.Fps)) % _iconFrames.Count;
            if (index != _previewFrame)
            {
                _previewFrame = index;
                frame = _iconFrames[index];
            }
        }

        foreach (var (image, scale, move, glow) in _previewParts)
        {
            if (frame != null)
            {
                image.Source = frame;
            }

            switch (icon.IconEffect)
            {
                case "pulse":
                    scale.ScaleX = scale.ScaleY = 1 + 0.12 * Math.Sin(t * 2 * Math.PI * 1.1);
                    break;
                case "flip" when _iconFrames.Count <= 1:
                    scale.ScaleX = Math.Cos(t * Math.PI * 0.9);
                    break;
                case "float":
                    move.Y = -image.Height * 0.12 * Math.Sin(t * 2 * Math.PI * 0.7);
                    break;
                case "glow" when glow != null:
                    glow.Opacity = 0.55 + 0.35 * (0.5 + 0.5 * Math.Sin(t * 2 * Math.PI * 0.8));
                    break;
            }
        }
    }

    // ---------------------------------------------------------------- Animated thumbnails

    // Animated icons play in the galleries, so the animated flame looks unlike the still one
    private void PlayGalleries(IEnumerable<GalleryItem> items)
    {
        var t = (DateTime.UtcNow - IconClock).TotalSeconds;
        foreach (var item in items)
        {
            if (item.Animated)
            {
                item.ShowFrame(t, 8);
            }
        }
    }

    // ---------------------------------------------------------------- The frame loop

    // The preview icon and the gallery thumbnails move on WPF's own frame, as the animated names
    // (StyledName) do. They used DispatcherTimers, which run at Background priority, below input:
    // moving the mouse over the gallery (a hover and a tooltip per tile) kept the dispatcher busy
    // and the icon froze until the mouse stopped. The loop runs only while something moves.
    private bool _iconAnimating;
    private bool _frameLoop;

    private void EnsureFrameLoop()
    {
        if (!_frameLoop)
        {
            CompositionTarget.Rendering += OnFrame;
            _frameLoop = true;
        }
    }

    private void StopFrameLoop()
    {
        if (_frameLoop)
        {
            CompositionTarget.Rendering -= OnFrame;
            _frameLoop = false;
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var gallery = IconsPage.Visibility == Visibility.Visible ? _gallery : BotsPage.Visibility == Visibility.Visible ? _botIcons : null;
        var galleryMoving = gallery != null && gallery.Any(i => i.Animated);
        var iconMoving = _iconAnimating && IconsPage.Visibility == Visibility.Visible;
        if (!iconMoving && !galleryMoving)
        {
            StopFrameLoop();
            return;
        }

        if (!IsVisible || WindowState == WindowState.Minimized)
        {
            return;
        }

        if (iconMoving)
        {
            PlayIcon();
        }

        if (galleryMoving)
        {
            PlayGalleries(gallery!);
        }
    }
}
