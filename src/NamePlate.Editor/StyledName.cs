using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using NamePlate.Shared;

namespace NamePlate.Editor;

/// <summary>Looks on the letters: outline and glow colors (null for off), shadow, font style.</summary>
public record NameFx(Color? Outline = null, Color? Glow = null, bool Shadow = false, bool Bold = false, bool Italic = false, bool SmallCaps = false)
{
    public bool Any => Outline != null || Glow != null || Shadow || Bold || Italic || SmallCaps;
}

/// <summary>
/// How a nickname is colored and moves. The math is the shared NamePaint, the same code the plugin
/// runs, so the preview is what the game draws.
/// </summary>
public record NameLook(IReadOnlyList<Color> Colors, NameMode Mode, double Speed, string? Motion = null, bool Reverse = false, NameFx? Fx = null, string? Tag = null,
    string? TagColor = null)
{
    public static NameLook Solid(Color color) => new([color], NameMode.Solid, 0);

    /// <summary>The tag is painted with the name's colors, as the game does for "tagColor": "name".</summary>
    public bool TagInName => Tag != null && string.Equals(TagColor, TagColors.Name, StringComparison.OrdinalIgnoreCase);

    public bool Moving => ToPaint().Moving;

    /// <summary>One color on a solid name, all of them otherwise.</summary>
    public NamePaint ToPaint()
    {
        var colors = Mode == NameMode.Solid && Colors.Count > 0 ? [Colors[0]] : Colors;
        var rgb = colors.Select(c => new[] { c.R / 255.0, c.G / 255.0, c.B / 255.0 }).ToArray();
        return new NamePaint(rgb, Mode == NameMode.Letters, Motion, Speed, Reverse);
    }
}

/// <summary>
/// A nickname drawn in its colors: one color, a gradient across the name or one per letter, still
/// or moving, with a clan tag, outline, glow or shadow as the game draws them.
/// </summary>
public class StyledName : FrameworkElement
{
    // The tag's color in the game (NameColorizer.TagColor)
    private static readonly Color TagColor = Color.FromRgb(168, 179, 186);

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StyledName), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LookProperty = DependencyProperty.Register(
        nameof(Look), typeof(NameLook), typeof(StyledName), new FrameworkPropertyMetadata(NameLook.Solid(Colors.White),
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((StyledName)d).LookChanged()));

    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(StyledName), new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((StyledName)d).UpdateGlow()));

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        nameof(FontFamily), typeof(FontFamily), typeof(StyledName), new FrameworkPropertyMetadata(new FontFamily("Segoe UI Variable Text, Segoe UI"), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register(
        nameof(FontWeight), typeof(FontWeight), typeof(StyledName), new FrameworkPropertyMetadata(FontWeights.SemiBold, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public NameLook Look { get => (NameLook)GetValue(LookProperty); set => SetValue(LookProperty, value); }
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public FontFamily FontFamily { get => (FontFamily)GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
    public FontWeight FontWeight { get => (FontWeight)GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }

    private static readonly DateTime Start = DateTime.UtcNow;
    private NamePaint _paint = NameLook.Solid(Colors.White).ToPaint();
    private bool _animating;

    public StyledName()
    {
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => StopAnimation();
        IsVisibleChanged += (_, _) => UpdateAnimation();
    }

    private void LookChanged()
    {
        _paint = (Look ?? NameLook.Solid(Colors.White)).ToPaint();
        UpdateGlow();
        UpdateAnimation();
    }

    // Glow and shadow are a real blur behind the letters (the game's are a soft underlay); drawn
    // with strokes they came out as a muddy band
    private void UpdateGlow()
    {
        var fx = Fx;
        Effect = fx.Glow is { } glow
            ? new DropShadowEffect { Color = glow, ShadowDepth = 0, BlurRadius = Math.Max(4, FontSize * 0.55), Opacity = 1 }
            : fx.Shadow
                ? new DropShadowEffect { Color = Colors.Black, ShadowDepth = Math.Max(1, FontSize * 0.07), Direction = 315, BlurRadius = Math.Max(2, FontSize * 0.12), Opacity = 0.85 }
                : null;
    }

    private void UpdateAnimation()
    {
        if (_paint.Moving && IsLoaded && IsVisible)
        {
            if (!_animating)
            {
                CompositionTarget.Rendering += OnFrame;
                _animating = true;
            }
        }
        else
        {
            StopAnimation();
        }
    }

    private void StopAnimation()
    {
        if (_animating)
        {
            CompositionTarget.Rendering -= OnFrame;
            _animating = false;
        }
    }

    private void OnFrame(object? sender, EventArgs e) => InvalidateVisual();

    // ---------------------------------------------------------------- layout

    private NameFx Fx => Look?.Fx ?? new NameFx();

    // The text as shown, with the tag in brackets first; and where the name starts in it
    private (string Shown, int NameStart) Shown()
    {
        var name = Text ?? "";
        var tag = Look?.Tag;
        return tag != null ? ($"[{tag}] {name}", tag.Length + 3) : (name, 0);
    }

    private Typeface Typeface(NameFx fx) =>
        new(FontFamily, fx.Italic ? FontStyles.Italic : FontStyles.Normal, fx.Bold ? FontWeights.Bold : FontWeight, FontStretches.Normal);

    // Small caps as capitals, the lowercase ones a size smaller
    private FormattedText Format(string shown, NameFx fx, Brush brush)
    {
        var text = new FormattedText(fx.SmallCaps ? shown.ToUpperInvariant() : shown, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            Typeface(fx), FontSize, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (fx.SmallCaps)
        {
            for (var i = 0; i < shown.Length; i++)
            {
                if (char.IsLower(shown[i]))
                {
                    text.SetFontSize(FontSize * 0.8, i, 1);
                }
            }
        }

        return text;
    }

    // Only the text takes up room. An outline, glow, shadow or a hop draws past the edges, as in
    // game, so turning one on never resizes or moves what's around the name.
    protected override Size MeasureOverride(Size availableSize)
    {
        var (shown, _) = Shown();
        var text = Format(shown, Fx, Brushes.White);
        return new Size(Math.Ceiling(text.WidthIncludingTrailingWhitespace), Math.Ceiling(text.Height));
    }

    // ---------------------------------------------------------------- drawing

    protected override void OnRender(DrawingContext dc)
    {
        var fx = Fx;
        var paint = _paint;
        var (shown, tagEnd) = Shown();

        // A tag in the name's colors is painted as the name's first letters
        var nameStart = Look?.TagInName == true ? 0 : tagEnd;
        var origin = new Point(0, 0);
        var text = Format(shown, fx, Brushes.White);
        var time = (DateTime.UtcNow - Start).TotalSeconds;
        var rgb = new double[3];

        // Where each visible letter of the name sits
        var letters = new List<(int Index, double Left, double Right)>();
        for (var i = nameStart; i < shown.Length; i++)
        {
            if (char.IsWhiteSpace(shown[i]))
            {
                continue;
            }

            var bounds = text.BuildHighlightGeometry(new Point(0, 0), i, 1)?.Bounds;
            if (bounds is { IsEmpty: false } b)
            {
                letters.Add((i, b.Left, b.Right));
            }
        }

        var minX = letters.Count > 0 ? letters.Min(l => l.Left) : 0;
        var width = Math.Max((letters.Count > 0 ? letters.Max(l => l.Right) : 1) - minX, 0.001);

        // Each letter gets the colors at its left and right edges, as the plugin colors each
        // letter's corners in game
        var brushes = new List<Brush>();
        for (var n = 0; n < letters.Count; n++)
        {
            var (_, left, right) = letters[n];
            paint.At((left - minX) / width, n, time, rgb);
            var c0 = ToColor(rgb);
            paint.At((right - minX) / width, n, time, rgb);
            var c1 = ToColor(rgb);
            Brush brush = c0 == c1
                ? new SolidColorBrush(c0)
                : new LinearGradientBrush(c0, c1, new Point(left, 0), new Point(right, 0)) { MappingMode = BrushMappingMode.Absolute };
            brush.Freeze();
            brushes.Add(brush);
        }

        var tagBrush = new SolidColorBrush(Images.TryParseColor(Look?.TagColor, out var ownTag) ? ownTag : TagColor);
        tagBrush.Freeze();
        for (var i = 0; i < nameStart; i++)
        {
            text.SetForegroundBrush(tagBrush, i, 1);
        }

        if (!paint.MovesLetters)
        {
            for (var n = 0; n < letters.Count; n++)
            {
                text.SetForegroundBrush(brushes[n], letters[n].Index, 1);
            }

            DrawEffects(dc, text.BuildGeometry(origin), fx);
            dc.DrawText(text, origin);
            return;
        }

        // Moving letters: the tag where it is, then each letter of the name at its own offset
        if (nameStart > 0)
        {
            var tag = Format(shown[..nameStart], fx, tagBrush);
            DrawEffects(dc, tag.BuildGeometry(origin), fx);
            dc.DrawText(tag, origin);
        }

        for (var n = 0; n < letters.Count; n++)
        {
            var (index, left, _) = letters[n];
            paint.Offset(n, time, out var dx, out var dy);
            var letter = Format(shown.Substring(index, 1), fx, brushes[n]);
            if (fx.SmallCaps && char.IsLower(shown[index]))
            {
                letter.SetFontSize(FontSize * 0.8);
            }

            // The letter's brush is laid out along the whole name, so draw it at its own place
            // there and move the drawing instead
            dc.PushTransform(new TranslateTransform(origin.X + dx * FontSize, origin.Y - dy * FontSize));
            DrawEffects(dc, letter.BuildGeometry(new Point(left, 0)), fx);
            dc.DrawText(letter, new Point(left, 0));
            dc.Pop();
        }
    }

    // The outline, under the letters (glow and shadow are the element's Effect, see UpdateGlow)
    private void DrawEffects(DrawingContext dc, Geometry geometry, NameFx fx)
    {
        if (fx.Outline is { } outline)
        {
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(outline), Math.Max(1.5, FontSize * 0.12)) { LineJoin = PenLineJoin.Round }, geometry);
        }
    }

    private static Color ToColor(double[] rgb) =>
        Color.FromRgb(Byte(rgb[0]), Byte(rgb[1]), Byte(rgb[2]));

    private static byte Byte(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
}
